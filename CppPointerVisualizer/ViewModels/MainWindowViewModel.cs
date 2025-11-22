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
        /// Используется упрощённый Sugiyama: вычисление слоёв (длина пути), затем два прохода барицентров для
        /// уменьшения пересечений, после чего постановка в сетку с отступами.
        /// </summary>
        public void AutoRoute()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("AUTO-ROUTE STARTED");
            Console.WriteLine("========================================");
            
            if (NodeViewModels.Count == 0)
            {
                Console.WriteLine("No nodes to route. Exiting.");
                return;
            }

            Console.WriteLine($"Total NodeViewModels: {NodeViewModels.Count}");

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

            // Вспомогательные списки предков/потомков
            var preds = addressMap.Values.ToDictionary(n => n, _ => new List<MemoryNodeViewModel>());
            var succs = addressMap.Values.ToDictionary(n => n, _ => new List<MemoryNodeViewModel>());
            foreach (var (from, to) in edges)
            {
                preds[to].Add(from);
                succs[from].Add(to);
            }

            // Вычисляем слои по максимальной длине пути (корни на слое 0).
            var depthMap = new Dictionary<MemoryNodeViewModel, int>();
            int Depth(MemoryNodeViewModel node, HashSet<MemoryNodeViewModel> visiting)
            {
                if (depthMap.TryGetValue(node, out var cached))
                    return cached;

                // Защита от циклов: при цикле считаем глубину 0.
                if (!visiting.Add(node))
                    return 0;

                var parents = preds[node];
                var depth = parents.Count == 0 ? 0 : parents.Max(p => Depth(p, visiting)) + 1;
                depthMap[node] = depth;
                visiting.Remove(node);
                return depth;
            }

            foreach (var node in NodeViewModels)
                Depth(node, new HashSet<MemoryNodeViewModel>());

            // Группируем по слоям и задаём начальный порядок
            var layers = depthMap.GroupBy(kv => kv.Value)
                                 .OrderBy(g => g.Key)
                                 .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).OrderBy(n => n.Name).ToList());

            Console.WriteLine($"\nLayer structure created:");
            foreach (var (layerIndex, layerNodes) in layers.OrderBy(kv => kv.Key))
            {
                Console.WriteLine($"  Layer {layerIndex}: {layerNodes.Count} nodes - {string.Join(", ", layerNodes.Select(n => n.Name ?? "unnamed"))}");
            }

            // Улучшенная эвристика: используем медиану вместо среднего для более стабильного результата
            // Три прохода для лучшего качества
            for (int pass = 0; pass < 3; pass++)
            {
                // Вниз: ориентируемся на предков (используем медиану позиций)
                foreach (var layerIndex in layers.Keys.OrderBy(i => i))
                {
                    var layer = layers[layerIndex];
                    var ordered = layer.OrderBy(n =>
                    {
                        var pr = preds[n];
                        if (pr.Count == 0) return double.MaxValue;
                        
                        // Используем медиану вместо среднего для более стабильного результата
                        var positions = pr.Select(p => (double)layers[depthMap[p]].IndexOf(p)).OrderBy(x => x).ToList();
                        return positions.Count % 2 == 0
                            ? (positions[positions.Count / 2 - 1] + positions[positions.Count / 2]) / 2.0
                            : positions[positions.Count / 2];
                    }).ToList();
                    layers[layerIndex] = ordered;
                }

                // Вверх: ориентируемся на потомков (используем медиану позиций)
                foreach (var layerIndex in layers.Keys.OrderByDescending(i => i))
                {
                    var layer = layers[layerIndex];
                    var ordered = layer.OrderBy(n =>
                    {
                        var ch = succs[n];
                        if (ch.Count == 0) return double.MaxValue;
                        
                        // Используем медиану вместо среднего
                        var positions = ch.Select(c => (double)layers[depthMap[c]].IndexOf(c)).OrderBy(x => x).ToList();
                        return positions.Count % 2 == 0
                            ? (positions[positions.Count / 2 - 1] + positions[positions.Count / 2]) / 2.0
                            : positions[positions.Count / 2];
                    }).ToList();
                    layers[layerIndex] = ordered;
                }
            }

            // Умный адаптивный расчет spacing на основе структуры графа
            var (xSpacing, ySpacing, margin) = CalculateAdaptiveSpacing(layers, edges);
            
            // Вычисляем максимальное количество узлов в слое для центрирования
            var maxNodesInLayer = layers.Values.Max(l => l.Count);
            
            // Расстановка координат с адаптивным spacing и вертикальным центрированием
            foreach (var (layerIndex, layerNodes) in layers.OrderBy(kv => kv.Key))
            {
                double x = margin + layerIndex * xSpacing;
                
                // Вычисляем вертикальное смещение для центрирования слоя
                double totalLayerHeight = (layerNodes.Count - 1) * ySpacing;
                double maxLayerHeight = (maxNodesInLayer - 1) * ySpacing;
                double verticalOffset = (maxLayerHeight - totalLayerHeight) / 2.0;
                
                double y = margin + verticalOffset;
                
                Console.WriteLine($"Layer {layerIndex}: {layerNodes.Count} nodes, vertical offset = {verticalOffset:F1}");
                
                foreach (var node in layerNodes)
                {
                    node.Position = new Point(x, y);
                    Console.WriteLine($"  {node.Name ?? "unnamed"}: ({x:F1}, {y:F1})");
                    y += ySpacing;
                }
            }

            Console.WriteLine($"\nCoordinates assigned (before normalization):");
            Console.WriteLine($"  Min X: {NodeViewModels.Min(n => n.Position.X):F1}, Max X: {NodeViewModels.Max(n => n.Position.X):F1}");
            Console.WriteLine($"  Min Y: {NodeViewModels.Min(n => n.Position.Y):F1}, Max Y: {NodeViewModels.Max(n => n.Position.Y):F1}");

            // Нормализуем координаты относительно margin
            var minX = NodeViewModels.Min(n => n.Position.X);
            var minY = NodeViewModels.Min(n => n.Position.Y);
            var shiftX = margin - minX;
            var shiftY = margin - minY;

            if (Math.Abs(shiftX) > double.Epsilon || Math.Abs(shiftY) > double.Epsilon)
            {
                Console.WriteLine($"\nApplying normalization shift: X={shiftX:F1}, Y={shiftY:F1}");
                foreach (var node in NodeViewModels)
                {
                    node.Position = new Point(node.Position.X + shiftX, node.Position.Y + shiftY);
                }
            }

            Console.WriteLine($"\nFinal node positions:");
            foreach (var node in NodeViewModels.Take(10)) // Show first 10 nodes
            {
                Console.WriteLine($"  {node.Name ?? "unnamed"}: ({node.Position.X:F1}, {node.Position.Y:F1})");
            }
            if (NodeViewModels.Count > 10)
                Console.WriteLine($"  ... and {NodeViewModels.Count - 10} more nodes");

            // Финальная оптимизация: выравнивание узлов с одинаковым количеством связей
            OptimizeNodeAlignment(layers, depthMap, preds, succs);
            
            Console.WriteLine("========================================");
            Console.WriteLine("AUTO-ROUTE COMPLETED");
            Console.WriteLine("========================================\n");
        }

        /// <summary>
        /// Умный расчет адаптивного spacing на основе структуры графа
        /// </summary>
        private (double xSpacing, double ySpacing, double margin) CalculateAdaptiveSpacing(
            Dictionary<int, List<MemoryNodeViewModel>> layers,
            List<(MemoryNodeViewModel from, MemoryNodeViewModel to)> edges)
        {
            // 1. Вычисляем максимальную ширину узла
            double maxNodeWidth = 0;
            
            foreach (var layer in layers.Values)
            {
                foreach (var node in layer)
                {
                    double nodeWidth = node.Width;
                    
                    // Если ширина еще не рассчитана (0), оцениваем её по содержимому
                    if (nodeWidth <= 1.0)
                    {
                        // Примерная оценка: макс. длина строки * 8 пикселей + отступы
                        int maxLen = Math.Max(node.Name?.Length ?? 0, node.TypeDescription?.Length ?? 0);
                        maxLen = Math.Max(maxLen, node.ValueOrTarget?.Length ?? 0);
                        maxLen = Math.Max(maxLen, node.Address?.Length ?? 0);
                        maxLen = Math.Max(maxLen, node.Modifiability?.Length ?? 0);
                        
                        // Минимум 150px, плюс 8px на символ
                        nodeWidth = 150 + maxLen * 8;
                    }
                    
                    if (nodeWidth > maxNodeWidth)
                        maxNodeWidth = nodeWidth;
                }
            }
            
            Console.WriteLine($"Max Node Width: {maxNodeWidth:F1}px");

            // Базовые параметры на основе ширины узлов
            double minXSpacing = maxNodeWidth + 80; // Ширина узла + отступ 80px
            double maxXSpacing = minXSpacing * 1.5;
            
            const double minYSpacing = 150;
            const double maxYSpacing = 220;
            const double baseMargin = 50;

            // Анализ структуры графа
            int totalNodes = layers.Values.Sum(l => l.Count);
            int layerCount = layers.Count;
            int maxNodesInLayer = layers.Values.Max(l => l.Count);
            double avgNodesPerLayer = totalNodes / (double)layerCount;
            
            // Плотность связей (edges per node)
            double edgeDensity = totalNodes > 0 ? edges.Count / (double)totalNodes : 1.0;

            Console.WriteLine("=== ADAPTIVE SPACING CALCULATION ===");
            Console.WriteLine($"Total Nodes: {totalNodes}");
            Console.WriteLine($"Layer Count: {layerCount}");
            Console.WriteLine($"Max Nodes in Layer: {maxNodesInLayer}");
            Console.WriteLine($"Avg Nodes per Layer: {avgNodesPerLayer:F2}");
            Console.WriteLine($"Total Edges: {edges.Count}");
            Console.WriteLine($"Edge Density (edges/node): {edgeDensity:F2}");

            // 1. Адаптивный X-spacing (горизонтальный)
            // Больше слоев = меньше spacing для компактности
            // Больше связей = больше spacing для читаемости
            double xSpacingFactor = 1.0;
            
            if (layerCount <= 3)
            {
                xSpacingFactor = 1.3; // Широкий spacing для простых графов
                Console.WriteLine($"X-Spacing Factor: 1.3 (simple graph, {layerCount} layers)");
            }
            else if (layerCount <= 6)
            {
                xSpacingFactor = 1.1; // Средний spacing
                Console.WriteLine($"X-Spacing Factor: 1.1 (medium graph, {layerCount} layers)");
            }
            else
            {
                xSpacingFactor = 0.9; // Компактный spacing для сложных графов
                Console.WriteLine($"X-Spacing Factor: 0.9 (complex graph, {layerCount} layers)");
            }

            // Увеличиваем spacing при высокой плотности связей
            if (edgeDensity > 2.0)
            {
                xSpacingFactor *= 1.2;
                Console.WriteLine($"X-Spacing Factor adjusted to {xSpacingFactor:F2} (high edge density: {edgeDensity:F2})");
            }
            else if (edgeDensity > 1.5)
            {
                xSpacingFactor *= 1.1;
                Console.WriteLine($"X-Spacing Factor adjusted to {xSpacingFactor:F2} (medium edge density: {edgeDensity:F2})");
            }

            double xSpacing = Math.Clamp(minXSpacing * xSpacingFactor, minXSpacing, maxXSpacing);
            Console.WriteLine($"Final X-Spacing: {xSpacing:F1}px (min: {minXSpacing}, max: {maxXSpacing})");

            // 2. Адаптивный Y-spacing (вертикальный)
            // Больше узлов в слое = меньше spacing для компактности
            // Меньше узлов = больше spacing для визуального комфорта
            double ySpacingFactor = 1.0;

            if (maxNodesInLayer <= 2)
            {
                ySpacingFactor = 1.4; // Большой spacing для малого количества узлов
                Console.WriteLine($"Y-Spacing Factor: 1.4 (few nodes, max {maxNodesInLayer})");
            }
            else if (maxNodesInLayer <= 4)
            {
                ySpacingFactor = 1.2; // Средний spacing
                Console.WriteLine($"Y-Spacing Factor: 1.2 (medium nodes, max {maxNodesInLayer})");
            }
            else if (maxNodesInLayer <= 8)
            {
                ySpacingFactor = 1.0; // Базовый spacing
                Console.WriteLine($"Y-Spacing Factor: 1.0 (many nodes, max {maxNodesInLayer})");
            }
            else
            {
                ySpacingFactor = 0.85; // Компактный spacing для многих узлов
                Console.WriteLine($"Y-Spacing Factor: 0.85 (very many nodes, max {maxNodesInLayer})");
            }

            // Учитываем общее количество узлов
            if (totalNodes > 20)
            {
                ySpacingFactor *= 0.9; // Более компактно для больших графов
                Console.WriteLine($"Y-Spacing Factor adjusted to {ySpacingFactor:F2} (large graph, {totalNodes} nodes)");
            }
            else if (totalNodes < 5)
            {
                ySpacingFactor *= 1.2; // Более просторно для маленьких графов
                Console.WriteLine($"Y-Spacing Factor adjusted to {ySpacingFactor:F2} (small graph, {totalNodes} nodes)");
            }

            double ySpacing = Math.Clamp(minYSpacing * ySpacingFactor, minYSpacing, maxYSpacing);
            Console.WriteLine($"Final Y-Spacing: {ySpacing:F1}px (min: {minYSpacing}, max: {maxYSpacing})");

            // 3. Адаптивный margin
            // Больше граф = больше margin для визуального баланса
            double margin = baseMargin;
            if (totalNodes > 15 || layerCount > 5)
            {
                margin = 60;
                Console.WriteLine($"Margin: {margin}px (large graph)");
            }
            else if (totalNodes < 5)
            {
                margin = 40;
                Console.WriteLine($"Margin: {margin}px (small graph)");
            }
            else
            {
                Console.WriteLine($"Margin: {margin}px (default)");
            }

            Console.WriteLine($"=== RESULT: X={xSpacing:F1}, Y={ySpacing:F1}, Margin={margin} ===");
            Console.WriteLine();

            return (xSpacing, ySpacing, margin);
        }

        /// <summary>
        /// Оптимизирует выравнивание узлов для улучшения визуального баланса
        /// </summary>
        private void OptimizeNodeAlignment(
            Dictionary<int, List<MemoryNodeViewModel>> layers,
            Dictionary<MemoryNodeViewModel, int> depthMap,
            Dictionary<MemoryNodeViewModel, List<MemoryNodeViewModel>> preds,
            Dictionary<MemoryNodeViewModel, List<MemoryNodeViewModel>> succs)
        {
            const double alignmentThreshold = 20.0;

            foreach (var (layerIndex, layerNodes) in layers)
            {
                if (layerNodes.Count < 2) continue;

                // Для узлов с одним предком и одним потомком, выравниваем их вертикально
                foreach (var node in layerNodes)
                {
                    var nodeSuccs = succs[node];
                    var nodePreds = preds[node];

                    // Если у узла ровно один предок и один потомок, выравниваем по вертикали
                    if (nodePreds.Count == 1 && nodeSuccs.Count == 1)
                    {
                        var pred = nodePreds[0];
                        var succ = nodeSuccs[0];
                        
                        // Вычисляем среднюю Y-позицию между предком и потомком
                        var targetY = (pred.Position.Y + succ.Position.Y) / 2.0;
                        
                        // Применяем выравнивание только если разница небольшая
                        if (Math.Abs(node.Position.Y - targetY) < alignmentThreshold)
                        {
                            node.Position = new Point(node.Position.X, targetY);
                        }
                    }
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
