namespace IFramework.Specifications
{
    /// <summary>
    ///     Represents the base class for all the composite specifications.
    /// </summary>
    public abstract class CompositeSpecification<T> : Specification<T>, ICompositeSpecification<T>
        //  where T : class, IEntity
    {
        #region Ctor

        /// <summary>
        ///     Constructs a new instance of the composite specification.
        /// </summary>
        /// <param name="left">The left side of the specification.</param>
        /// <param name="right">The right side of the specification.</param>
        public CompositeSpecification(ISpecification<T> left, ISpecification<T> right)
        {
            Left = left;
            Right = right;
        }

        #endregion

        #region Private Fields

        #endregion

        #region ICompositeSpecification Members

        /// <summary>
        ///     Gets the left side of the specification.
        /// </summary>
        public ISpecification<T> Left { get; }

        /// <summary>
        ///     Gets the right side of the specification.
        /// </summary>
        public ISpecification<T> Right { get; }

        #endregion
    }
}