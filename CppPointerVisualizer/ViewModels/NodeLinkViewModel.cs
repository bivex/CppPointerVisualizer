using System;

namespace CppPointerVisualizer.ViewModels
{
    /// <summary>
    /// ViewModel для связи между узлами
    /// </summary>
    public class NodeLinkViewModel : ViewModelBase
    {
        private Guid _guid = Guid.NewGuid();
        private Guid _outputConnectorGuid;
        private Guid _inputConnectorGuid;
        private bool _isLocked;
        private bool _isSelected;

        public Guid Guid
        {
            get => _guid;
            set => SetProperty(ref _guid, value);
        }

        public Guid OutputConnectorGuid
        {
            get => _outputConnectorGuid;
            set => SetProperty(ref _outputConnectorGuid, value);
        }

        public Guid InputConnectorGuid
        {
            get => _inputConnectorGuid;
            set => SetProperty(ref _inputConnectorGuid, value);
        }

        public bool IsLocked
        {
            get => _isLocked;
            set => SetProperty(ref _isLocked, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
