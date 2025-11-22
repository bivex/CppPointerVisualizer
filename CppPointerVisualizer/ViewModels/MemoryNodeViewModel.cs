using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CppPointerVisualizer.Models;

namespace CppPointerVisualizer.ViewModels
{
    /// <summary>
    /// ViewModel для узла памяти (переменная, указатель или ссылка)
    /// </summary>
    public class MemoryNodeViewModel : ViewModelBase
    {
        private double _width;
        private double _height;
        private Guid _guid = Guid.NewGuid();
        private Point _position;
        private bool _isSelected;
        private MemoryObject? _memoryObject;

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public Guid Guid
        {
            get => _guid;
            set => SetProperty(ref _guid, value);
        }

        public Point Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public MemoryObject? MemoryObject
        {
            get => _memoryObject;
            set
            {
                SetProperty(ref _memoryObject, value);
                UpdateFromMemoryObject();
            }
        }

        // Свойства для отображения в UI
        public string Name { get; private set; } = string.Empty;
        public string TypeDescription { get; private set; } = string.Empty;
        public string ValueOrTarget { get; private set; } = string.Empty;
        public string Address { get; private set; } = string.Empty;
        public string Modifiability { get; private set; } = string.Empty;

        public ObservableCollection<NodeInputViewModel> Inputs { get; } = new ObservableCollection<NodeInputViewModel>();
        public ObservableCollection<NodeOutputViewModel> Outputs { get; } = new ObservableCollection<NodeOutputViewModel>();

        public IEnumerable<NodeConnectorViewModel> InputsEnumerable => Inputs;
        public IEnumerable<NodeConnectorViewModel> OutputsEnumerable => Outputs;

        public ICommand SizeChangedCommand { get; }

        public MemoryNodeViewModel()
        {
            SizeChangedCommand = new RelayCommand<Size>(size =>
            {
                Width = size.Width;
                Height = size.Height;
            });

            // Все узлы имеют входной порт (на них можно указывать)
            Inputs.Add(new NodeInputViewModel("Address", true));
        }

        private void UpdateFromMemoryObject()
        {
            if (_memoryObject == null) return;

            Name = _memoryObject.Name;
            TypeDescription = _memoryObject.GetTypeDescription();
            Address = _memoryObject.Address;
            Modifiability = _memoryObject.GetModifiabilityInfo();

            if (_memoryObject.ObjectType == MemoryObjectType.Variable)
            {
                ValueOrTarget = $"Value: {_memoryObject.Value}";
            }
            else if (_memoryObject.ObjectType == MemoryObjectType.Pointer)
            {
                ValueOrTarget = _memoryObject.PointsTo == "nullptr"
                    ? "→ nullptr"
                    : $"→ {_memoryObject.PointsTo}";
            }
            else if (_memoryObject.ObjectType == MemoryObjectType.Reference)
            {
                ValueOrTarget = $"→ {_memoryObject.PointsTo}";
            }

            // Если это указатель или ссылка, добавляем выходной порт
            Outputs.Clear();
            if (_memoryObject.ObjectType == MemoryObjectType.Pointer ||
                _memoryObject.ObjectType == MemoryObjectType.Reference)
            {
                if (!string.IsNullOrEmpty(_memoryObject.PointsTo) && _memoryObject.PointsTo != "nullptr")
                {
                    Outputs.Add(new NodeOutputViewModel("Points To"));
                }
            }

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(TypeDescription));
            OnPropertyChanged(nameof(ValueOrTarget));
            OnPropertyChanged(nameof(Address));
            OnPropertyChanged(nameof(Modifiability));
            OnPropertyChanged(nameof(OutputsEnumerable));
        }

        public NodeConnectorViewModel? FindConnector(Guid guid)
        {
            var input = Inputs.FirstOrDefault(c => c.Guid == guid);
            if (input != null)
                return input;

            var output = Outputs.FirstOrDefault(c => c.Guid == guid);
            return output;
        }
    }

    /// <summary>
    /// Простая команда для MVVM без внешних зависимостей
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null)
                return true;

            return parameter is T typedParam && _canExecute(typedParam);
        }

        public void Execute(object? parameter)
        {
            if (parameter is T typedParam)
                _execute(typedParam);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
