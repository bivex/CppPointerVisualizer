using System;

namespace CppPointerVisualizer.ViewModels
{
    /// <summary>
    /// ViewModel для коннектора узла (входной или выходной порт)
    /// </summary>
    public class NodeConnectorViewModel : ViewModelBase
    {
        private Guid _guid = Guid.NewGuid();
        private string _label = string.Empty;
        private bool _isEnable = true;

        public Guid Guid
        {
            get => _guid;
            set => SetProperty(ref _guid, value);
        }

        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        public bool IsEnable
        {
            get => _isEnable;
            set => SetProperty(ref _isEnable, value);
        }
    }

    /// <summary>
    /// ViewModel для входного коннектора
    /// </summary>
    public class NodeInputViewModel : NodeConnectorViewModel
    {
        private bool _allowToConnectMultiple;

        public bool AllowToConnectMultiple
        {
            get => _allowToConnectMultiple;
            set => SetProperty(ref _allowToConnectMultiple, value);
        }

        public NodeInputViewModel(string label, bool allowMultiple = false)
        {
            Label = label;
            AllowToConnectMultiple = allowMultiple;
        }
    }

    /// <summary>
    /// ViewModel для выходного коннектора
    /// </summary>
    public class NodeOutputViewModel : NodeConnectorViewModel
    {
        public NodeOutputViewModel(string label)
        {
            Label = label;
        }
    }
}
