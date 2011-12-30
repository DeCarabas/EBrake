namespace EncoderUI.Controls
{
    using System.Windows.Controls;
    using System.Windows.Input;

    public class FixedFlowDocumentViewer : FlowDocumentScrollViewer
    {
        protected override void OnMouseWheel(MouseWheelEventArgs e) 
        { 
            // Ignore 
        }
    }
}
