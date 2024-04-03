namespace Robowire.RobOrm.Core.NonOrm
{
    public interface ISqlBuilder
    {
        /// <summary>
        /// Determines that this sql command will be call to a stored procedure
        /// </summary>
        /// <param name="storedProcedureName">Name of the procedure</param>
        /// <returns></returns>
        ISqlExecutor Call(string storedProcedureName);

        /// <summary>
        /// Sets commandType = Text and sets the text of the command
        /// </summary>
        /// <param name="sql">Command text</param>
        /// <returns></returns>
        ISqlExecutor Execute(string sql);

        /// <summary>
        /// Sets text of the command with positions of parameters marked by placeholders "{0}, {1}, ..."
        /// Placeholders will be replaced by generated parameter names.
        /// For example: .ExecuteWithParams("SELECT * FROM cls_Contact WHERE email = {0}", "epitest@inwk.com")..
        /// Note that values are not injected into command text but set to parameters.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        ISqlExecutor ExecuteWithParams(string sql, params object[] parameter);
    }
}
