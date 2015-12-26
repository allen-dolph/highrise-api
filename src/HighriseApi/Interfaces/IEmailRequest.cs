using System;
using System.Collections.Generic;
using HighriseApi.Models.Enums;

namespace HighriseApi.Interfaces
{
    public interface IEmailRequest
    {
        #region Get

        /// <summary>
        /// Gets a collection of Emails that are visible to the authenticated user.
        /// </summary>
        /// <returns>An IEnumerable collection of <see cref="Email"/> objects</returns>        
        IEnumerable<Email> Get(SubjectType subjectType, int subjectId, int? offset = null);

        /// <summary>
        /// Gets a collection of people that have been created or updated since the date passed in.
        /// </summary>
        /// <param name="subjectId"></param>
        /// <param name="startDate">The date after which a user must be created in order to be returned</param>
        /// <param name="subjectType"></param>
        /// <param name="offset"></param>
        /// <returns>An IEnumerable collection of <see cref="Email"/> objects</returns>
        IEnumerable<Email> Get(SubjectType subjectType, int subjectId, DateTime startDate, int? offset = null);

        #endregion
    }
}
