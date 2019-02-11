﻿//
//  Copyright (C) 2016 DataStax, Inc.
//
//  Please see the license for details:
//  http://www.datastax.com/terms/datastax-dse-driver-license-terms
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace Dse.Geometry
{
    /// <summary>
    /// Represents a one-dimensional object representing a sequence of points and the line segments connecting them.
    /// </summary>
#if NET45
    [Serializable]
#endif
    public class LineString : GeometryBase
    {
        private static readonly Regex WktRegex = new Regex(
            @"^LINESTRING ?\(([-0-9\. ,]+)\)+$", RegexOptions.Compiled);

        /// <summary>
        /// Gets the read-only list of points describing the LineString.
        /// </summary>
        public IList<Point> Points { get; private set; }

        /// <inheritdoc />
        protected override IEnumerable GeoCoordinates
        {
            get { return Points.Select(p => new[] { p.X, p.Y }); }
        }

        /// <summary>
        /// Creates a new instance of <see cref="LineString"/> using a sequence of points.
        /// </summary>
        public LineString(params Point[] points) : this((IList<Point>)points)
        {

        }

#if NET45
        /// <summary>
        /// Creates a new instance of <see cref="LineString"/> using a serialization information.
        /// </summary>
        protected LineString(SerializationInfo info, StreamingContext context)
        {
            var coordinates = (double[][])info.GetValue("coordinates", typeof(double[][]));
            Points = AsReadOnlyCollection(coordinates.Select(arr => new Point(arr[0], arr[1])).ToArray());
        }
#endif

        /// <summary>
        /// Creates a new instance of <see cref="LineString"/> using a list of points.
        /// </summary>
        public LineString(IList<Point> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }
            if (points.Count == 1)
            {
                throw new ArgumentOutOfRangeException("points", "LineString can be either empty or contain 2 or more points");
            }
            Points = AsReadOnlyCollection(points);
        }

        /// <summary>
        /// Returns a value indicating whether this instance and a specified object represent the same value.
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as LineString;
            if (other == null)
            {
                return false;
            }
            if (Points.Count != other.Points.Count)
            {
                return false;
            }
            return !(Points.Where((t, i) => !t.Equals(other.Points[i])).Any());
        }

        /// <summary>
        /// Returns the hash code based on the value of this instance.
        /// </summary>
        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return CombineHashCode(Points);
        }

        /// <summary>
        /// Returns Well-known text (WKT) representation of the geometry object.
        /// </summary>
        public override string ToString()
        {
            if (Points.Count == 0)
            {
                return "LINESTRING EMPTY";
            }
            return string.Format("LINESTRING ({0})", string.Join(", ", Points.Select(p => p.X + " " + p.Y)));
        }

        /// <summary>
        /// Creates a <see cref="LineString"/> instance from a 
        /// <see href="https://en.wikipedia.org/wiki/Well-known_text">Well-known Text(WKT)</see>
        /// representation of a line.
        /// </summary>
        public static LineString Parse(string textValue)
        {
            if (textValue == null)
            {
                throw new ArgumentNullException("textValue");
            }
            if (textValue == "LINESTRING EMPTY")
            {
                return new LineString();
            }
            var match = WktRegex.Match(textValue);
            if (!match.Success)
            {
                throw InvalidFormatException(textValue);
            }
            var points = ParseSegments(match.Groups[1].Value);
            return new LineString(points);
        }

        internal static Point[] ParseSegments(string textValue)
        {
            var pointParts = textValue.Split(',');
            var points = new Point[pointParts.Length];
            for (var i = 0; i < pointParts.Length; i++)
            {
                var p = pointParts[i].Trim();
                if (p.Length == 0)
                {
                    throw InvalidFormatException(textValue);
                }
                var xyText = p.Split(' ').Select(e => e.Trim()).Where(e => e.Length > 0).ToArray();
                if (xyText.Length != 2)
                {
                    throw InvalidFormatException(textValue);
                }
                points[i] = new Point(Convert.ToDouble(xyText[0]), Convert.ToDouble(xyText[1]));
            }
            return points;
        }
    }
}
