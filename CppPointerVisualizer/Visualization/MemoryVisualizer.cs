using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CppPointerVisualizer.Models;

namespace CppPointerVisualizer.Visualization
{
    public class MemoryVisualizer
    {
        private readonly Canvas _canvas;
        private const double BoxWidth = 200;
        private const double BoxHeight = 120;
        private const double BoxSpacing = 50;
        private const double StartX = 50;
        private const double StartY = 50;

        public MemoryVisualizer(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void Visualize(MemoryState state)
        {
            _canvas.Children.Clear();

            if (state.Objects.Count == 0)
            {
                _canvas.Width = 800;
                _canvas.Height = 600;
                return;
            }

            double currentY = StartY;
            var objectPositions = new Dictionary<string, Point>();

            // Рисуем все объекты
            foreach (var obj in state.Objects)
            {
                var position = new Point(StartX, currentY);
                objectPositions[obj.Address] = position;

                DrawMemoryObject(obj, position);

                currentY += BoxHeight + BoxSpacing;
            }

            // Устанавливаем размер Canvas динамически
            double canvasWidth = StartX + BoxWidth + 400 + 50; // Box + info text + margin
            double canvasHeight = currentY + 50; // Final Y + bottom margin
            _canvas.Width = canvasWidth;
            _canvas.Height = canvasHeight;

            // Рисуем стрелки для указателей и ссылок
            foreach (var obj in state.Objects)
            {
                if (obj.PointsTo != null && obj.PointsTo != "nullptr" && objectPositions.ContainsKey(obj.PointsTo))
                {
                    var fromPos = objectPositions[obj.Address];
                    var toPos = objectPositions[obj.PointsTo];

                    DrawArrow(fromPos, toPos, obj.ObjectType);
                }
            }
        }

        private void DrawMemoryObject(MemoryObject obj, Point position)
        {
            // Основной прямоугольник
            var rect = new Rectangle
            {
                Width = BoxWidth,
                Height = BoxHeight,
                Fill = GetObjectColor(obj),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                RadiusX = 5,
                RadiusY = 5
            };

            Canvas.SetLeft(rect, position.X);
            Canvas.SetTop(rect, position.Y);
            _canvas.Children.Add(rect);

            // Имя переменной
            var nameText = new TextBlock
            {
                Text = obj.Name,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center
            };

            Canvas.SetLeft(nameText, position.X + 10);
            Canvas.SetTop(nameText, position.Y + 10);
            _canvas.Children.Add(nameText);

            // Тип
            var typeText = new TextBlock
            {
                Text = obj.GetTypeDescription(),
                FontSize = 12,
                Foreground = Brushes.LightGray,
                TextWrapping = TextWrapping.Wrap,
                Width = BoxWidth - 20,
                TextAlignment = TextAlignment.Center
            };

            Canvas.SetLeft(typeText, position.X + 10);
            Canvas.SetTop(typeText, position.Y + 35);
            _canvas.Children.Add(typeText);

            // Значение или адрес
            string valueStr = "";
            if (obj.ObjectType == MemoryObjectType.Variable)
            {
                valueStr = $"Значение: {obj.Value}";
            }
            else if (obj.ObjectType == MemoryObjectType.Pointer)
            {
                valueStr = obj.PointsTo == "nullptr" ? "nullptr" : $"→ {obj.PointsTo}";
            }
            else if (obj.ObjectType == MemoryObjectType.Reference)
            {
                valueStr = $"→ {obj.PointsTo}";
            }

            var valueText = new TextBlock
            {
                Text = valueStr,
                FontSize = 12,
                Foreground = Brushes.Yellow,
                TextAlignment = TextAlignment.Center
            };

            Canvas.SetLeft(valueText, position.X + 10);
            Canvas.SetTop(valueText, position.Y + 60);
            _canvas.Children.Add(valueText);

            // Адрес в памяти
            var addrText = new TextBlock
            {
                Text = $"Адрес: {obj.Address}",
                FontSize = 10,
                Foreground = Brushes.DarkGray,
                TextAlignment = TextAlignment.Center
            };

            Canvas.SetLeft(addrText, position.X + 10);
            Canvas.SetTop(addrText, position.Y + 85);
            _canvas.Children.Add(addrText);

            // Информация о модифицируемости (справа от объекта)
            var modText = new TextBlock
            {
                Text = obj.GetModifiabilityInfo(),
                FontSize = 11,
                Foreground = Brushes.Cyan,
                TextWrapping = TextWrapping.Wrap,
                Width = 350
            };

            Canvas.SetLeft(modText, position.X + BoxWidth + 20);
            Canvas.SetTop(modText, position.Y + 30);
            _canvas.Children.Add(modText);
        }

        private Brush GetObjectColor(MemoryObject obj)
        {
            return obj.ObjectType switch
            {
                MemoryObjectType.Variable => new SolidColorBrush(Color.FromRgb(52, 73, 94)),
                MemoryObjectType.Pointer => new SolidColorBrush(Color.FromRgb(142, 68, 173)),
                MemoryObjectType.Reference => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                _ => Brushes.Gray
            };
        }

        private void DrawArrow(Point from, Point to, MemoryObjectType type)
        {
            var brush = type == MemoryObjectType.Reference ? Brushes.LimeGreen : Brushes.Magenta;

            // Определяем направление стрелки
            bool isUpward = to.Y < from.Y;
            bool isSameLevel = Math.Abs(to.Y - from.Y) < 10;

            // Вычисляем точки подключения
            Point startPoint, endPoint;

            if (isSameLevel)
            {
                // Стрелка на одном уровне - справа налево
                startPoint = new Point(from.X + BoxWidth, from.Y + BoxHeight / 2);
                endPoint = new Point(to.X, to.Y + BoxHeight / 2);
                DrawStraightArrow(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y, brush);
            }
            else
            {
                // Автоматическая маршрутизация для разных уровней
                DrawSmartRoutedArrow(from, to, isUpward, brush);
            }
        }

        private void DrawSmartRoutedArrow(Point from, Point to, bool isUpward, Brush brush)
        {
            // Начальная точка - справа от источника
            Point startPoint = new Point(from.X + BoxWidth, from.Y + BoxHeight / 2);

            // Конечная точка зависит от направления
            Point endPoint;
            if (isUpward)
            {
                // Стрелка вверх - входим снизу
                endPoint = new Point(to.X + BoxWidth / 2, to.Y + BoxHeight);
            }
            else
            {
                // Стрелка вниз - входим сверху
                endPoint = new Point(to.X + BoxWidth / 2, to.Y);
            }

            // Создаем путь с автоматической маршрутизацией
            var path = new System.Windows.Shapes.Path
            {
                Stroke = brush,
                StrokeThickness = 3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 2,
                    Opacity = 0.3,
                    BlurRadius = 3
                }
            };

            // Создаем маршрут с промежуточными точками
            var pathFigure = new PathFigure { StartPoint = startPoint };

            // Расстояние для огибания (адаптивное)
            double offset = Math.Min(80, Math.Abs(endPoint.Y - startPoint.Y) * 0.4);
            double cornerRadius = 15; // Радиус закругления углов

            // Промежуточные точки для маршрутизации
            Point p1 = new Point(startPoint.X + offset, startPoint.Y);
            Point p2 = new Point(p1.X, endPoint.Y);
            Point p3 = endPoint;

            if (isUpward)
            {
                // Маршрут вверх с закругленными углами
                // Горизонтальный выход вправо
                pathFigure.Segments.Add(new LineSegment(
                    new Point(p1.X - cornerRadius, startPoint.Y), true));

                // Закругление вниз-вправо
                pathFigure.Segments.Add(new ArcSegment(
                    new Point(p1.X, startPoint.Y + (startPoint.Y > p2.Y ? -cornerRadius : cornerRadius)),
                    new Size(cornerRadius, cornerRadius),
                    0, false, SweepDirection.Clockwise, true));

                // Вертикальный участок
                pathFigure.Segments.Add(new LineSegment(
                    new Point(p1.X, p2.Y + cornerRadius), true));

                // Закругление влево
                pathFigure.Segments.Add(new ArcSegment(
                    new Point(p1.X - cornerRadius, p2.Y),
                    new Size(cornerRadius, cornerRadius),
                    0, false, SweepDirection.Clockwise, true));

                // Горизонтальный подход к цели
                pathFigure.Segments.Add(new LineSegment(
                    new Point(p3.X + cornerRadius, p2.Y), true));

                // Закругление вниз
                pathFigure.Segments.Add(new ArcSegment(
                    new Point(p3.X, p2.Y + cornerRadius),
                    new Size(cornerRadius, cornerRadius),
                    0, false, SweepDirection.Clockwise, true));

                // Финальный спуск к цели
                pathFigure.Segments.Add(new LineSegment(p3, true));
            }
            else
            {
                // Маршрут вниз с плавной кривой
                pathFigure.Segments.Add(new BezierSegment(
                    new Point(startPoint.X + offset, startPoint.Y),
                    new Point(endPoint.X + offset, endPoint.Y),
                    endPoint, true));
            }

            var geometry = new PathGeometry();
            geometry.Figures.Add(pathFigure);
            path.Data = geometry;

            _canvas.Children.Add(path);

            // Рисуем наконечник стрелки
            double angle = -Math.PI / 2; // Направление вниз

            DrawArrowHead(endPoint.X, endPoint.Y, angle, brush);
        }

        private void DrawStraightArrow(double x1, double y1, double x2, double y2, Brush brush)
        {
            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = brush,
                StrokeThickness = 3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 2,
                    Opacity = 0.3,
                    BlurRadius = 3
                }
            };

            _canvas.Children.Add(line);

            // Стрелка
            DrawArrowHead(x2, y2, Math.Atan2(y2 - y1, x2 - x1), brush);
        }

        private void DrawArrowHead(double x, double y, double angle, Brush brush)
        {
            const double arrowLength = 18;
            const double arrowAngle = Math.PI / 5.5;

            // Создаем наконечник стрелки с помощью Path для более качественного вида
            var pathFigure = new PathFigure
            {
                StartPoint = new Point(x, y),
                IsClosed = true
            };

            pathFigure.Segments.Add(new LineSegment(
                new Point(
                    x - arrowLength * Math.Cos(angle - arrowAngle),
                    y - arrowLength * Math.Sin(angle - arrowAngle)
                ), true));

            pathFigure.Segments.Add(new LineSegment(
                new Point(
                    x - arrowLength * Math.Cos(angle + arrowAngle),
                    y - arrowLength * Math.Sin(angle + arrowAngle)
                ), true));

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            var arrowPath = new System.Windows.Shapes.Path
            {
                Data = pathGeometry,
                Fill = brush,
                Stroke = brush,
                StrokeThickness = 1,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 1,
                    Opacity = 0.4,
                    BlurRadius = 2
                }
            };

            _canvas.Children.Add(arrowPath);
        }
    }
}
