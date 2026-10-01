using System;
using System.Collections.Generic;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Core.Extensions;

namespace Stanchions
{
    public class StanchionsBuilder
    {
        private Model _model;
        private PluginData _data;
        private Point _start;
        private Point _end;
        private List<Part> _parts;

        public StanchionsBuilder(Model model, PluginData data, Point start, Point end, List<Part> parts)
        {
            _model = model;
            _data = data;
            _start = start;
            _end = end;
            _parts = parts;
        }

        public int Build()
        {
            int postCount = 0;
            double startOffsetMm = _data.StartOffset;
            double endOffsetMm = _data.EndOffset;
            double offsetMm = _data.Offset;
            double spanMm = _data.Span;
            double heightMm = _data.Height;

            Vector dir = _start.GetDirectionTo(_end);
            double totalLength = dir.GetLength();
            if (totalLength < 1e-6) return 0;
            dir = dir.GetNormal();

            if (Math.Abs(offsetMm) > 1e-6)
            {
                Vector lateral = dir.GetPerpendicular();
                if (lateral.GetLength() < 1e-6) lateral = new Vector(0, 1, 0);
                lateral = lateral.GetNormal();
                
                _start = _start.MoveTowards(lateral, offsetMm);
                _end = _end.MoveTowards(lateral, offsetMm);
            }

            double usableLength = totalLength - startOffsetMm - endOffsetMm;
            List<double> stations = new List<double>();

            if (usableLength > 1e-3)
            {
                if (_data.SpaceScheme == 1) // Max span
                {
                    stations.Add(startOffsetMm);
                    int spaces = (int)Math.Ceiling(usableLength / spanMm);
                    if (spaces <= 0) spaces = 1;
                    double actualSpan = usableLength / spaces;
                    for (int i = 1; i < spaces; i++)
                    {
                        stations.Add(startOffsetMm + i * actualSpan);
                    }
                    stations.Add(startOffsetMm + usableLength);
                }
                else // Exact span
                {
                    if (spanMm > 1e-3)
                    {
                        int spaces = (int)Math.Floor(usableLength / spanMm);
                        double totalSpanned = spaces * spanMm;
                        double remainder = usableLength - totalSpanned;
                        double firstPostDist = startOffsetMm + (remainder / 2.0);
                        
                        for (int i = 0; i <= spaces; i++)
                        {
                            stations.Add(firstPostDist + i * spanMm);
                        }
                    }
                }
            }
            else
            {
                stations.Add(totalLength / 2.0); 
            }

            for (int i = 0; i < stations.Count; i++)
            {
                double dist = stations[i];
                Point pt = _start.MoveTowards(dir, dist);
                
                if (TrySeatOnBase(pt, out Point seatPt))
                {
                    bool isFirst = (i == 0);
                    bool isLast = (i == stations.Count - 1);
                    InsertPlumbPost(seatPt, heightMm, isFirst, isLast);
                    postCount++;
                }
            }
            
            return postCount;
        }

        private bool TrySeatOnBase(Point xyPt, out Point seatPt)
        {
            seatPt = new Point(xyPt);
            if (_parts.Count == 0) return false;

            double maxZ = double.MinValue;
            bool found = false;

            Point[] offsets = { 
                new Point(0, 0, 0),
                new Point(1, 0, 0),
                new Point(-1, 0, 0),
                new Point(0, 1, 0),
                new Point(0, -1, 0)
            };

            foreach (var offset in offsets)
            {
                Point top = new Point(xyPt.X + offset.X, xyPt.Y + offset.Y, 1.0e6);
                Point bottom = new Point(xyPt.X + offset.X, xyPt.Y + offset.Y, -1.0e6);

                foreach (var part in _parts)
                {
                    var solid = part.GetSolid();
                    if (solid == null) continue;
                    var intersections = solid.Intersect(bottom, top);
                    if (intersections != null)
                    {
                        foreach (Point p in intersections)
                        {
                            if (p.Z > maxZ)
                            {
                                maxZ = p.Z;
                                found = true;
                            }
                        }
                    }
                }
            }

            if (found)
            {
                seatPt.Z = maxZ;
                return true;
            }
            return false;
        }

        private void InsertPlumbPost(Point basePt, double height, bool isFirst, bool isLast)
        {
            Beam post = new Beam(Beam.BeamTypeEnum.COLUMN);
            post.StartPoint = basePt;
            post.EndPoint = new Point(basePt.X, basePt.Y, basePt.Z + height);

            string prof, mat, cls, name;

            if (isFirst)
            {
                prof = _data.FirstProfile; mat = _data.FirstMaterial; cls = _data.ClassFirst; name = _data.FirstPartName;
            }
            else if (isLast)
            {
                if (_data.LastProfileLink == 1) // Match First
                {
                    prof = _data.FirstProfile; mat = _data.FirstMaterial; cls = _data.ClassFirst; name = _data.FirstPartName;
                }
                else if (_data.LastProfileLink == 2) // Match Middle
                {
                    if (_data.MidProfileLink == 1) // Middle matches First
                    {
                        prof = _data.FirstProfile; mat = _data.FirstMaterial; cls = _data.ClassFirst; name = _data.FirstPartName;
                    }
                    else
                    {
                        prof = _data.MidProfile; mat = _data.MidMaterial; cls = _data.ClassMid; name = _data.MidPartName;
                    }
                }
                else // Custom
                {
                    prof = _data.LastProfile; mat = _data.LastMaterial; cls = _data.ClassLast; name = _data.LastPartName;
                }
            }
            else // Middle
            {
                if (_data.MidProfileLink == 1) // Match First
                {
                    prof = _data.FirstProfile; mat = _data.FirstMaterial; cls = _data.ClassFirst; name = _data.FirstPartName;
                }
                else if (_data.MidProfileLink == 2) // Match Last
                {
                    if (_data.LastProfileLink == 1) // Last matches First
                    {
                        prof = _data.FirstProfile; mat = _data.FirstMaterial; cls = _data.ClassFirst; name = _data.FirstPartName;
                    }
                    else
                    {
                        prof = _data.LastProfile; mat = _data.LastMaterial; cls = _data.ClassLast; name = _data.LastPartName;
                    }
                }
                else // Custom
                {
                    prof = _data.MidProfile; mat = _data.MidMaterial; cls = _data.ClassMid; name = _data.MidPartName;
                }
            }

            post.Profile.ProfileString = prof;
            post.Material.MaterialString = mat;
            post.Class = cls;
            post.Name = name;

            post.AssemblyNumber.Prefix = "";
            post.AssemblyNumber.StartNumber = 1;
            post.PartNumber.Prefix = "r";
            post.PartNumber.StartNumber = 100;

            post.Position.Depth = Position.DepthEnum.MIDDLE;
            post.Position.Plane = Position.PlaneEnum.MIDDLE;
            post.Position.Rotation = Position.RotationEnum.FRONT;

            post.Insert();
        }
    }
}

