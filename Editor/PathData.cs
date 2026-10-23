using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Reads SVG path data, the format of Android's icon masks, into polygons: one per subpath, curves and arcs cut into
    ///     line segments.
    /// </summary>
    internal static class PathData
    {
        private const int CURVE_SEGMENTS = 16;
        private const float ARC_SEGMENT_ANGLE = Mathf.PI / 32;

        private static readonly Regex _tokens = new Regex(@"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?");

        /// <summary>
        ///     Polygons of the path data, each subpath closed or not; filling closes them.
        /// </summary>
        internal static List<Vector2[]> Flatten(string data)
        {
            var path = new Polygons();
            var command = 'M';
            var numbers = new List<float>();
            foreach (Match match in _tokens.Matches(data))
            {
                if (!char.IsLetter(match.Value[0]))
                {
                    numbers.Add(float.Parse(match.Value, CultureInfo.InvariantCulture));
                    continue;
                }

                Run(path, command, numbers);
                command = match.Value[0];
                numbers.Clear();
            }

            Run(path, command, numbers);
            return path.Finish();
        }

        /// <summary>
        ///     Runs one command with its numbers, repeated as long as numbers are left; a move followed by more points draws
        ///     lines to them.
        /// </summary>
        private static void Run(Polygons path, char command, List<float> numbers)
        {
            var relative = char.IsLower(command);
            var type = char.ToUpperInvariant(command);
            if (type == 'Z')
            {
                path.Close();
                return;
            }

            var count = ArgumentCount(type);
            for (var i = 0; i + count <= numbers.Count; i += count)
            {
                var origin = relative ? path.Current : Vector2.zero;
                switch (type)
                {
                    case 'M':
                        if (i == 0) path.MoveTo(origin + Point(numbers, i));
                        else path.LineTo(origin + Point(numbers, i));
                        break;
                    case 'L':
                        path.LineTo(origin + Point(numbers, i));
                        break;
                    case 'H':
                        path.LineTo(new Vector2(numbers[i] + origin.x, path.Current.y));
                        break;
                    case 'V':
                        path.LineTo(new Vector2(path.Current.x, numbers[i] + origin.y));
                        break;
                    case 'C':
                        path.CubicTo(origin + Point(numbers, i), origin + Point(numbers, i + 2), origin + Point(numbers, i + 4));
                        break;
                    case 'S':
                        path.CubicTo(path.ReflectedControl('C'), origin + Point(numbers, i), origin + Point(numbers, i + 2));
                        break;
                    case 'Q':
                        path.QuadraticTo(origin + Point(numbers, i), origin + Point(numbers, i + 2));
                        break;
                    case 'T':
                        path.QuadraticTo(path.ReflectedControl('Q'), origin + Point(numbers, i));
                        break;
                    case 'A':
                        path.ArcTo(numbers[i], numbers[i + 1], numbers[i + 2], numbers[i + 3] != 0, numbers[i + 4] != 0, origin + Point(numbers, i + 5));
                        break;
                }
            }
        }

        private static int ArgumentCount(char type)
        {
            switch (type)
            {
                case 'H':
                case 'V':
                    return 1;
                case 'C':
                    return 6;
                case 'S':
                case 'Q':
                    return 4;
                case 'A':
                    return 7;
                default:
                    return 2;
            }
        }

        private static Vector2 Point(List<float> numbers, int index)
        {
            return new Vector2(numbers[index], numbers[index + 1]);
        }

        /// <summary>
        ///     Polygons being built, with the state of the path: current point, start of the subpath and last control point.
        /// </summary>
        private class Polygons
        {
            private readonly List<Vector2[]> _polygons = new List<Vector2[]>();
            private readonly List<Vector2> _points = new List<Vector2>();
            private Vector2 _start;
            private Vector2 _lastControl;
            private char _lastCurve;

            internal Vector2 Current { get; private set; }

            internal void MoveTo(Vector2 point)
            {
                EndSubpath();
                _points.Add(point);
                _start = point;
                SetCurrent(point, ' ');
            }

            internal void LineTo(Vector2 point)
            {
                AddPoint(point);
                SetCurrent(point, ' ');
            }

            internal void CubicTo(Vector2 control1, Vector2 control2, Vector2 end)
            {
                var start = Current;
                for (var i = 1; i <= CURVE_SEGMENTS; i++)
                {
                    var t = (float)i / CURVE_SEGMENTS;
                    var u = 1 - t;
                    AddPoint(u * u * u * start + 3 * u * u * t * control1 + 3 * u * t * t * control2 + t * t * t * end);
                }

                _lastControl = control2;
                SetCurrent(end, 'C');
            }

            internal void QuadraticTo(Vector2 control, Vector2 end)
            {
                var start = Current;
                for (var i = 1; i <= CURVE_SEGMENTS; i++)
                {
                    var t = (float)i / CURVE_SEGMENTS;
                    var u = 1 - t;
                    AddPoint(u * u * start + 2 * u * t * control + t * t * end);
                }

                _lastControl = control;
                SetCurrent(end, 'Q');
            }

            /// <summary>
            ///     Elliptical arc to a point, from the SVG endpoint parameters, converted to its centre and angles as the SVG
            ///     specification does; radii too small to reach the point grow until they do.
            /// </summary>
            internal void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 end)
            {
                var start = Current;
                if (start == end) return;

                var rx = Mathf.Abs(radiusX);
                var ry = Mathf.Abs(radiusY);
                if (rx == 0 || ry == 0)
                {
                    LineTo(end);
                    return;
                }

                var angle = rotation * Mathf.Deg2Rad;
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);
                var half = (start - end) / 2;
                var x1 = cos * half.x + sin * half.y;
                var y1 = -sin * half.x + cos * half.y;
                var lambda = x1 * x1 / (rx * rx) + y1 * y1 / (ry * ry);
                if (lambda > 1)
                {
                    rx *= Mathf.Sqrt(lambda);
                    ry *= Mathf.Sqrt(lambda);
                }

                var numerator = rx * rx * ry * ry - rx * rx * y1 * y1 - ry * ry * x1 * x1;
                var denominator = rx * rx * y1 * y1 + ry * ry * x1 * x1;
                var factor = Mathf.Sqrt(Mathf.Max(0, numerator / denominator)) * (largeArc == sweep ? -1 : 1);
                var centreX = factor * rx * y1 / ry;
                var centreY = -factor * ry * x1 / rx;
                var centre = new Vector2(cos * centreX - sin * centreY, sin * centreX + cos * centreY) + (start + end) / 2;
                var startAngle = Angle(new Vector2(1, 0), new Vector2((x1 - centreX) / rx, (y1 - centreY) / ry));
                var deltaAngle = Angle(new Vector2((x1 - centreX) / rx, (y1 - centreY) / ry), new Vector2((-x1 - centreX) / rx, (-y1 - centreY) / ry));
                if (!sweep && deltaAngle > 0) deltaAngle -= 2 * Mathf.PI;
                else if (sweep && deltaAngle < 0) deltaAngle += 2 * Mathf.PI;

                var segments = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(deltaAngle) / ARC_SEGMENT_ANGLE));
                for (var i = 1; i < segments; i++)
                {
                    var t = startAngle + deltaAngle * i / segments;
                    var x = rx * Mathf.Cos(t);
                    var y = ry * Mathf.Sin(t);
                    AddPoint(centre + new Vector2(cos * x - sin * y, sin * x + cos * y));
                }

                AddPoint(end);
                SetCurrent(end, ' ');
            }

            /// <summary>
            ///     First control point of a smooth curve: the last control point mirrored through the current point after a
            ///     curve of the same kind, otherwise the current point.
            /// </summary>
            internal Vector2 ReflectedControl(char curve)
            {
                return _lastCurve == curve ? 2 * Current - _lastControl : Current;
            }

            internal void Close()
            {
                EndSubpath();
                SetCurrent(_start, ' ');
            }

            internal List<Vector2[]> Finish()
            {
                EndSubpath();
                return _polygons;
            }

            private void AddPoint(Vector2 point)
            {
                if (_points.Count == 0) _points.Add(Current);

                _points.Add(point);
            }

            private void SetCurrent(Vector2 point, char curve)
            {
                Current = point;
                _lastCurve = curve;
            }

            private void EndSubpath()
            {
                if (_points.Count > 2) _polygons.Add(_points.ToArray());

                _points.Clear();
            }

            private static float Angle(Vector2 from, Vector2 to)
            {
                return Mathf.Atan2(from.x * to.y - from.y * to.x, from.x * to.x + from.y * to.y);
            }
        }
    }
}