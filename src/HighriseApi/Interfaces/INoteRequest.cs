using HighriseApi.Models;
using HighriseApi.Models.Enums;
using System.Collections.Generic;

namespace HighriseApi.Interfaces
{
    /// <summary>
    /// Working with Notes (https://github.com/basecamp/highrise-api/blob/master/sections/notes.md)
    /// </summary>
    public interface INoteRequest
    {
        /// <summary>
        /// Gets a Note by Note ID.
        /// </summary>
        /// <returns>An instance of a <see cref="Note"/> object</returns>
        Note Get(int id);

        /// <summary>
        /// Gets an IEnumerable collection of <see cref="Note"/> objects by Subject Id and <see cref="SubjectType"/>.
        /// </summary>
        /// <param name="subjectId">SubjectId (Id of person, company, kase or deal)</param>
        /// <param name="subjectType">A <see cref="SubjectType"/> enum value</param>
        /// <returns>An IEnumerable collection of <see cref="Note"/> objects</returns>.
        IEnumerable<Note> Get(int subjectId, SubjectType subjectType);

        /// <summary>
        /// Creates a new note
        /// </summary>
        /// <param name="note">A <see cref="Note"/> object</param>
        /// <returns>A <see cref="Note"/> object, populated with new Highrise ID and CreatedAt/UpdatedAt values</returns>
        Note Create(Note note);

        /// <summary>
        /// Updates a Note
        /// </summary>
        /// <param name="note">A <see cref="Note"/> object</param>
        /// <returns>True if successfully updated, otherwise false</returns>
        bool Update(Note note);

        /// <summary>
        /// Deletes a note
        /// </summary>
        /// <param name="id">The ID of the note to delete</param>
        /// <returns>True if successfully deleted, otherwise false</returns>
        bool Delete(int id);
    }
}
