using System;

namespace CodeGeneration.Primitives.Internal
{
    internal class InvocationBuilder : IInvocationBuilder
    {
        private bool m_isFirstParam = true;

        private readonly ICodeBlockBuilder m_owner;

        public InvocationBuilder(ICodeBlockBuilder owner)
        {
            m_owner = owner;
        }

        public IInvocationBuilder WithParam(string value)
        {
            if (!m_isFirstParam)
            {
                m_owner.Write(", ");
            }
            m_isFirstParam = false;
            m_owner.Write(value);

            return this;
        }

        public IInvocationBuilder WithParam(INamedReference valueReference)
        {
            return WithParam(valueReference.Name);
        }

        public IInvocationBuilder WithParam(Action<ICodeBlockBuilder> paramCode)
        {
            if (!m_isFirstParam)
            {
                m_owner.Write(", ");
            }
            m_isFirstParam = false;

            paramCode(m_owner);

            return this;
        }
    }
}
