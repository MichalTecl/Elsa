-- Spustit v SSMS na produkcnim SQL Serveru BEHEM zpomaleni, PRED iisreset.
-- Ctrl+Shift+F: vysledky do souboru. Uchovat vsechny vysledkove sady.
-- Pouze diagnosticke SELECTy; zadne zmeny dat, KILL ani zmeny konfigurace.
-- Vyzaduje VIEW SERVER STATE (SQL Server 2022+: VIEW SERVER PERFORMANCE STATE).
-- Tri snimky s odstupem 5 sekund. SQL texty mohou obsahovat provozni data.
SET NOCOUNT ON;

SELECT SYSDATETIMEOFFSET() AS captured_at, @@SERVERNAME AS server_name,
       SERVERPROPERTY('ProductVersion') AS sql_version,
       sqlserver_start_time
FROM sys.dm_os_sys_info;

SELECT physical_memory_in_use_kb, locked_page_allocations_kb,
       available_commit_limit_kb, process_physical_memory_low,
       process_virtual_memory_low
FROM sys.dm_os_process_memory;

SELECT total_physical_memory_kb, available_physical_memory_kb,
       system_memory_state_desc
FROM sys.dm_os_sys_memory;

DECLARE @sample int = 1;
WHILE @sample <= 3
BEGIN
    DECLARE @capturedAt datetimeoffset = SYSDATETIMEOFFSET();

    -- A: Aktivni prikazy, cekani, otevrene transakce a blokujici sessions.
    -- Neomezovat na jednu databazi: blokujici session muze byt v jine DB.
    -- Zahrnuje i sleeping sessions s otevrenou transakci a jejich posledni SQL.
    SELECT @sample AS sample, @capturedAt AS captured_at,
           s.session_id, s.status AS session_status,
           s.host_name, s.host_process_id, s.program_name, s.login_name,
           c.connection_id AS server_connection_id, c.connect_time, c.last_read, c.last_write,
           s.open_transaction_count AS session_open_transactions,
           s.transaction_isolation_level,
           s.last_request_start_time, s.last_request_end_time,
           s.cpu_time AS session_cpu_ms, s.memory_usage AS session_memory_pages,
           r.request_id, r.status AS request_status, r.command,
           DB_NAME(r.database_id) AS request_database,
           r.blocking_session_id, r.wait_type, r.wait_time AS wait_ms,
           r.wait_resource, r.total_elapsed_time AS request_elapsed_ms,
           r.cpu_time AS request_cpu_ms, r.logical_reads, r.reads, r.writes,
           r.open_transaction_count AS request_open_transactions,
           CASE WHEN r.sql_handle IS NOT NULL THEN
               SUBSTRING(active_sql.text, r.statement_start_offset / 2 + 1,
                   (CASE WHEN r.statement_end_offset = -1 THEN DATALENGTH(active_sql.text)
                         ELSE r.statement_end_offset END - r.statement_start_offset) / 2 + 1)
           END AS current_statement,
           active_sql.text AS active_batch,
           recent_sql.text AS last_batch
    FROM sys.dm_exec_sessions s
    LEFT JOIN sys.dm_exec_requests r ON r.session_id = s.session_id
    LEFT JOIN sys.dm_exec_connections c ON c.session_id = s.session_id
         AND (r.connection_id IS NULL OR c.connection_id = r.connection_id)
    OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) active_sql
    OUTER APPLY sys.dm_exec_sql_text(c.most_recent_sql_handle) recent_sql
    WHERE s.is_user_process = 1
      AND s.session_id <> @@SPID
      AND (r.session_id IS NOT NULL OR s.open_transaction_count > 0
           OR EXISTS (SELECT 1 FROM sys.dm_exec_requests blocked
                      WHERE blocked.blocking_session_id = s.session_id))
    ORDER BY s.session_id, r.request_id;

    -- B: Stari a stav transakci; distribuovane i lokalni, vcetne neaktivnich.
    SELECT @sample AS sample, @capturedAt AS captured_at,
           st.session_id, s.host_name, s.host_process_id, s.program_name,
           s.status AS session_status, s.open_transaction_count,
           st.transaction_id, st.is_user_transaction, st.is_local,
           at.transaction_begin_time,
           DATEDIFF(SECOND, at.transaction_begin_time, GETDATE()) AS transaction_age_seconds,
           at.transaction_type, at.transaction_state,
           DB_NAME(dt.database_id) AS transaction_database,
           dt.database_transaction_log_bytes_used,
           dt.database_transaction_log_bytes_reserved
    FROM sys.dm_tran_session_transactions st
    JOIN sys.dm_exec_sessions s ON s.session_id = st.session_id
    LEFT JOIN sys.dm_tran_active_transactions at ON at.transaction_id = st.transaction_id
    LEFT JOIN sys.dm_tran_database_transactions dt ON dt.transaction_id = st.transaction_id
    WHERE s.is_user_process = 1 AND s.session_id <> @@SPID
    ORDER BY at.transaction_begin_time, st.session_id;

    -- C: Cekani jednotlivych SQL workeru (vcetne paralelnich vetvi).
    SELECT @sample AS sample, @capturedAt AS captured_at,
           w.session_id, w.exec_context_id, w.wait_duration_ms, w.wait_type,
           w.blocking_session_id, w.resource_description
    FROM sys.dm_os_waiting_tasks w
    JOIN sys.dm_exec_sessions s ON s.session_id = w.session_id
    WHERE s.is_user_process = 1 AND s.session_id <> @@SPID
    ORDER BY w.wait_duration_ms DESC;

    -- D: Tlak na SQL workery a CPU. Porovnat vsechny tri snimky.
    SELECT @sample AS sample, @capturedAt AS captured_at,
           scheduler_id, current_tasks_count, runnable_tasks_count,
           current_workers_count, active_workers_count, work_queue_count,
           pending_disk_io_count
    FROM sys.dm_os_schedulers
    WHERE status = 'VISIBLE ONLINE'
    ORDER BY scheduler_id;

    -- E: Pocet sessions podle procesu; sleeping samo o sobe neni unik spojeni.
    SELECT @sample AS sample, @capturedAt AS captured_at,
           host_name, host_process_id, program_name, status,
           COUNT(*) AS session_count,
           SUM(CASE WHEN open_transaction_count > 0 THEN 1 ELSE 0 END) AS sessions_with_transactions
    FROM sys.dm_exec_sessions
    WHERE is_user_process = 1 AND session_id <> @@SPID
    GROUP BY host_name, host_process_id, program_name, status
    ORDER BY session_count DESC;

    SET @sample = @sample + 1;
    IF @sample <= 3 WAITFOR DELAY '00:00:05';
END;
