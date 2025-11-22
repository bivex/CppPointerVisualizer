using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
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

        /// <summary>
        /// Автоматически раскладывает узлы по слоям, чтобы связи выглядели аккуратнее.
        /// Алгоритм располагает узлы с входящими связями правее тех, кто на них указывает.
        /// </summary>
        public void AutoRoute()
        {
            if (NodeViewModels.Count == 0)
                return;

            // Построим граф на основе MemoryObject (адрес -> PointsTo).
            var addressMap = NodeViewModels
                .Where(n => n.MemoryObject != null)
                .ToDictionary(n => n.MemoryObject!.Address, n => n);

            var edges = new List<(MemoryNodeViewModel from, MemoryNodeViewModel to)>();
            foreach (var node in addressMap.Values)
            {
                var targetAddress = node.MemoryObject?.PointsTo;
                if (string.IsNullOrEmpty(targetAddress) || targetAddress == "nullptr")
                    continue;

                if (addressMap.TryGetValue(targetAddress, out var targetNode))
                    edges.Add((node, targetNode));
            }

            var indegree = addressMap.Values.ToDictionary(n => n, _ => 0);
            foreach (var edge in edges)
            {
                if (indegree.ContainsKey(edge.to))
                    indegree[edge.to]++;
            }

            // Топологическая сортировка с максимальной глубиной
            var depthMap = new Dictionary<MemoryNodeViewModel, int>();
            var queue = new Queue<MemoryNodeViewModel>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var currentDepth = depthMap.TryGetValue(node, out var d) ? d : 0;

                foreach (var edge in edges.Where(e => e.from == node))
                {
                    var candidateDepth = currentDepth + 1;
                    if (!depthMap.TryGetValue(edge.to, out var existingDepth) || candidateDepth > existingDepth)
                        depthMap[edge.to] = candidateDepth;

                    indegree[edge.to]--;
                    if (indegree[edge.to] == 0)
                        queue.Enqueue(edge.to);
                }
            }

            // Незаписанные узлы считаем корневыми
            foreach (var node in NodeViewModels)
            {
                if (!depthMap.ContainsKey(node))
                    depthMap[node] = 0;
            }

            const double xSpacing = 260;
            const double ySpacing = 160;
            const double margin = 40;

            // Сначала ставим по слоям
            foreach (var group in depthMap.GroupBy(kv => kv.Value).OrderBy(g => g.Key))
            {
                var orderedNodes = group.Select(kv => kv.Key)
                                        .OrderBy(n => n.Name)
                                        .ToList();

                double y = margin;
                double x = margin + group.Key * xSpacing;

                foreach (var node in orderedNodes)
                {
                    node.Position = new Point(x, y);
                    y += ySpacing;
                }
            }

            // Нормализуем, чтобы минимальные координаты были не меньше margin.
            var minX = NodeViewModels.Min(n => n.Position.X);
            var minY = NodeViewModels.Min(n => n.Position.Y);
            var shiftX = margin - minX;
            var shiftY = margin - minY;

            if (Math.Abs(shiftX) > double.Epsilon || Math.Abs(shiftY) > double.Epsilon)
            {
                foreach (var node in NodeViewModels)
                {
                    node.Position = new Point(node.Position.X + shiftX, node.Position.Y + shiftY);
                }
            }
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
