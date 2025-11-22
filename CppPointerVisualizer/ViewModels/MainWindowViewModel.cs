using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CppPointerVisualizer.Models;

namespace CppPointerVisualizer.ViewModels
{
    /// <summary>
    /// ViewModel для главного окна, управляющий графом узлов памяти
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly Dictionary<string, MemoryNodeViewModel> _nodesByAddress = new();

        public ObservableCollection<MemoryNodeViewModel> NodeViewModels { get; } = new ObservableCollection<MemoryNodeViewModel>();
        public ObservableCollection<NodeLinkViewModel> NodeLinkViewModels { get; } = new ObservableCollection<NodeLinkViewModel>();

        /// <summary>
        /// Визуализирует состояние памяти
        /// </summary>
        public void Visualize(MemoryState state)
        {
            // Очищаем предыдущую визуализацию
            NodeViewModels.Clear();
            NodeLinkViewModels.Clear();
            _nodesByAddress.Clear();

            if (state?.Objects == null || state.Objects.Count == 0)
                return;

            // Создаем узлы для всех объектов
            double yPosition = 50;
            const double ySpacing = 180;

            foreach (var memObj in state.Objects)
            {
                var nodeVm = new MemoryNodeViewModel
                {
                    MemoryObject = memObj,
                    Position = new System.Windows.Point(100, yPosition)
                };

                // Используем адрес объекта для создания уникального GUID
                nodeVm.Guid = CreateGuidFromString(memObj.Address);

                NodeViewModels.Add(nodeVm);
                _nodesByAddress[memObj.Address] = nodeVm;

                yPosition += ySpacing;
            }

            // Создаем связи для указателей и ссылок
            CreateConnections(state);
        }

        private void CreateConnections(MemoryState state)
        {
            foreach (var memObj in state.Objects)
            {
                // Пропускаем объекты, которые не указывают на другие
                if (string.IsNullOrEmpty(memObj.PointsTo) || memObj.PointsTo == "nullptr")
                    continue;

                // Проверяем, существует ли целевой объект
                if (!_nodesByAddress.ContainsKey(memObj.Address) ||
                    !_nodesByAddress.ContainsKey(memObj.PointsTo))
                    continue;

                var sourceNode = _nodesByAddress[memObj.Address];
                var targetNode = _nodesByAddress[memObj.PointsTo];

                // Находим коннекторы
                var outputConnector = sourceNode.Outputs.FirstOrDefault();
                var inputConnector = targetNode.Inputs.FirstOrDefault();

                if (outputConnector != null && inputConnector != null)
                {
                    // Создаем соединение
                    var link = new NodeLinkViewModel
                    {
                        OutputConnectorGuid = outputConnector.Guid,
                        InputConnectorGuid = inputConnector.Guid
                    };

                    NodeLinkViewModels.Add(link);
                }
            }
        }

        private static Guid CreateGuidFromString(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return new Guid(hash);
        }
    }
}
