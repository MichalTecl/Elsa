using System.Text;

namespace Elsa.EditorBuilder.Internal
{
    internal class EditButtonColumnDefinition : GridColumnDefinition
    {
        public EditButtonColumnDefinition()
            : base(string.Empty, null, CellClass.Cell1, null)
        {
        }

        public override void RenderContent(StringBuilder target)
        {
            target.Append("<div class=\"cell1\"><i class=\"fas fa-edit faButton\" event-bind=\"click:editEntity(VM)\"></i></div>");
        }
    }
}
