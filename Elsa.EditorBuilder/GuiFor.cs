using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using Elsa.EditorBuilder.Content;
using Elsa.EditorBuilder.Internal;

using Robowire.RoboApi;

namespace Elsa.EditorBuilder
{
    public class GuiFor<T> : IDefineGrid<T>, IDefineForm<T>, ISetIdProperty<T>, ICanRenderGui
    {
        private Type m_controllerType;
        private string m_controllerUrl;
        private PropertyInfo m_idProperty;

        private GridBuilder<T> m_grid;
        private readonly IFormBuilder<T> m_form = new FormBuilder<T>();
        
        public IDefineForm<T> ShowsDataInGrid(Action<GridBuilder<T>> setupGrid)
        {
            setupGrid(m_grid);
            m_grid.EditActionColumn();
            return this;
        }
        
        private static ISetIdProperty<T> Calls(Type controllerType, string address = null)
        {
            var controllerAttribute = controllerType.GetCustomAttribute(typeof(ControllerAttribute)) as ControllerAttribute;
            if (controllerAttribute == null)
            {
                throw new InvalidOperationException(
                    $"{controllerType} is not marked by {typeof(ControllerAttribute)} attribute");
            }

            var builder = new GuiFor<T>
            {
                m_controllerType = controllerType,
                m_controllerUrl = address ?? $"/{controllerAttribute.Name}"
            };

            return builder;
        }

        public static ISetIdProperty<T> Calls(IAutoController<T> controller, string address = null)
        {
            return Calls(controller.GetType(), address);
        }

        public static ISetIdProperty<T> Calls<TController>(string address = null) where TController : IAutoController<T>
        {
            return Calls(typeof(TController), address);
        }

        public string Render()
        {
            var sb = new StringBuilder(ContentLoader.EditorTemplate);
            
            var typeName = (typeof(T).FullName ?? typeof(T).Name).Replace(".", "_").Replace(":","_").Replace(";", "_");
            var editorId = $"ctl_{typeName}";
            var vmName = $"vmForAutoGui_{typeName}";

            var formSb = new StringBuilder();
            ((ICanRender)m_form).Render(formSb);

            var gridSb = new StringBuilder();
            m_grid.Render(gridSb);

            sb.Replace("$vmName$", vmName)
              .Replace("$uidName$", m_idProperty.Name)
              .Replace("$controllerUrl$", m_controllerUrl)
              .Replace("$editForm$", formSb.ToString())
              .Replace("$grid$", gridSb.ToString())
              .Replace("$editorContainerName$", editorId);

            return sb.ToString();
        }

        public ICanRenderGui ProvidesEditForm(Action<IFormBuilder<T>> setupForm)
        {
            setupForm(m_form);

            return this;
        }

        public IDefineGrid<T> WithIdProperty<TId>(Expression<Func<T, TId>> property) 
        {
            if (default(TId) != null)
            {
                throw new ArgumentException("Id property must be nullable");
            }

            m_idProperty = ReflectionHelper.GetPropertyInfo(property);
            m_grid = new GridBuilder<T>(m_idProperty.Name);

            return this;
        }
    }

    public interface ISetIdProperty<T>
    {
        IDefineGrid<T> WithIdProperty<TId>(Expression<Func<T, TId>> property);
    }

    public interface IDefineForm<T>
    {
        ICanRenderGui ProvidesEditForm(Action<IFormBuilder<T>> setupForm);
    }

    public interface IDefineGrid<T>
    {
        IDefineForm<T> ShowsDataInGrid(Action<GridBuilder<T>> setupGrid);
    }

    public interface ICanRenderGui
    {
        string Render();
    }

}
