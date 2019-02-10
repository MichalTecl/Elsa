﻿using System;
using System.Linq;
using System.Web;
using System.Web.Routing;

using Elsa.Common;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RobOrm.Core;

namespace Elsa.Users
{
    public class UserWebSession : IWebSession
    {
        private const string c_sessionCookieIdentifier = "elsa_sid";
        private bool m_initialized;
        private Guid? m_sessionPublicId;

        private readonly IDatabase m_database;

        private IUserSession m_session;

        public UserWebSession(IDatabase database)
        {
            m_database = database;
        }

        public IUser User
        {
            get
            {
                EnsureInitialized();
                return m_session?.User;
            }
        }

        public IProject Project
        {
            get
            {
                EnsureInitialized();
                return m_session?.Project;
            }
        }

        public long? SessionId => m_session?.Id;

        public void Login(string user, string password)
        {
            EnsureInitialized();

            user = user?.Trim();
            password = password?.Trim();

            if (m_session?.User != null)
            {
                throw new InvalidOperationException("User already loged in");
            }

            var userRecord = m_database.SelectFrom<IUser>().Where(u => u.EMail == user).Execute().FirstOrDefault();
            if (userRecord == null)
            {
                return;
            }

            if (!VerifyPassword(userRecord.PasswordHash, password, userRecord.UsesDefaultPassword))
            {
                return;
            }

            m_session = m_database.New<IUserSession>();
            m_session.PublicId = m_sessionPublicId ?? Guid.Empty;
            m_session.UserId = userRecord.Id;
            m_session.ProjectId = userRecord.ProjectId;
            m_session.StartDt = DateTime.Now;
            m_session.EndDt = null;
            
            m_database.Save(m_session);

            m_session =
                m_database.SelectFrom<IUserSession>().Where(s => s.Id == m_session.Id).Join(s => s.User).Join(s => s.Project).Execute().FirstOrDefault();
        }

        public void Logout()
        {
            EnsureInitialized();

            if (User != null)
            {
                m_session.EndDt = DateTime.Now;
                m_database.Save(m_session);
                m_session = null;
            }
        }

        public bool VerifyPassword(string hash, string password, bool isDefault)
        {
            if (isDefault)
            {
                return hash == password;
            }

            return PasswordHashHelper.Verify(password, hash);
        }

        public void Initialize(RequestContext context)
        {
            var sessionCookie = context.HttpContext.Request.Cookies[c_sessionCookieIdentifier];
            if (sessionCookie != null)
            {
                Guid sid;
                if (Guid.TryParse(sessionCookie.Value, out sid))
                {
                    m_session =
                        m_database.SelectFrom<IUserSession>()
                            .Join(s => s.User)
                            .Join(s => s.Project)
                            //.Take(1)
                            .Where(s => (s.PublicId == sid) && (s.EndDt == null))
                            .Execute()
                            .FirstOrDefault();

                    if (m_session == null)
                    {
                        sessionCookie = null;
                    }
                }
                else
                {
                    sessionCookie = null;
                }
            }

            if (sessionCookie == null)
            {
                m_session = null;

                m_sessionPublicId = Guid.NewGuid();

                sessionCookie = new HttpCookie(c_sessionCookieIdentifier, m_sessionPublicId.ToString())
                                    {
                                        Expires = DateTime.Now
                                    };
            }

            sessionCookie.Expires = DateTime.Now.AddDays(1);
            context.HttpContext.Response.Cookies.Add(sessionCookie);
            
            m_initialized = true;
        }

        private void EnsureInitialized()
        {
            if (!m_initialized)
            {
                throw new InvalidOperationException("Session was not initialized");
            }
        }
    }
}



