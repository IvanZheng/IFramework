using IFramework.Exceptions;
using System;

namespace Sample.DTO.Exceptions
{
    public class UniqueConstraintException : DomainException
    {
        public UniqueConstraintException(Exception innerException,
                                         string message = null,
                                         string entityName = null,
                                         string indexName = null,
                                         string duplicatedValue = null)
            : base(DTO.ErrorCode.ConstraintCheckViolation, message ?? innerException.Message, innerException)
        {
            EntityName = entityName;
            IndexName = indexName;
            DuplicatedValue = duplicatedValue;
        }

        public string EntityName { get; set; }
        public string IndexName { get; set; }
        public string DuplicatedValue { get; set; }
    }
}
