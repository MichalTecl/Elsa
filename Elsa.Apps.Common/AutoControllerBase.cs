using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.EditorBuilder;
using Elsa.EditorBuilder.Internal;

using Robowire.RoboApi;

namespace Elsa.Apps.Common
{
    public abstract class AutoControllerBase<T> : ElsaControllerBase, IAutoController<T>
    {
        protected AutoControllerBase(IWebSession webSession, ILog log)
            : base(webSession, log)
        {
        }

        public abstract EntityListingPage<T> List(string pageKey);

        public abstract T Save(T entity);

        public abstract T Get(T uidHolder);

        public abstract T New();

        protected abstract IDefineGrid<T> SetUidProperty(ISetIdProperty<T> setter);

        protected abstract void SetupGrid(GridBuilder<T> gridBuilder);

        protected abstract void SetupForm(IFormBuilder<T> formBuilder);

        protected virtual string ControllerAddress { get; } = null;
        
        [RawString]
        [CanBeInherited]
        public string GetEditor()
        {
            return SetUidProperty(GuiFor<T>.Calls(this, ControllerAddress)).ShowsDataInGrid(SetupGrid).ProvidesEditForm(SetupForm).Render();
        }
        
        [CanBeInherited]
        public virtual IEnumerable<FieldValidationError> GetFieldErrors(T entity)
        {
            var context = new ValidationContext(entity);
            var results = new List<ValidationResult>();
            
            var isValid = Validator.TryValidateObject(entity, context, results);

            if (isValid)
            {
                yield break;
            }

            foreach (var r in results)
            {
                if (string.IsNullOrWhiteSpace(r.ErrorMessage))
                {
                    continue;
                }

                foreach (var f in r.MemberNames)
                {
                    yield return new FieldValidationError
                    {
                        Field = f,
                        Error = r.ErrorMessage
                    };
                }
            }
        }
    }
}
