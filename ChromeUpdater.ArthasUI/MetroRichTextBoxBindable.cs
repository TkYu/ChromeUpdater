using System.Windows;
using System.Windows.Documents;
using Arthas.Controls.Metro;

namespace ChromeUpdater.ArthasUI
{
    public class MetroRichTextBoxBindable : MetroRichTextBox
    {
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register("DocBinding", typeof(FlowDocument),
            typeof(MetroRichTextBoxBindable), new PropertyMetadata(OnDocumentChanged));
        public FlowDocument DocBinding
        {
            get { return (FlowDocument)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }
        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = (MetroRichTextBoxBindable)d;
            thisControl.Document = e.NewValue == null ? new FlowDocument() : (FlowDocument)e.NewValue;
        }
    }
}
