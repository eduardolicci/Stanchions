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
        private List<Point> _pathPoints;

        public StanchionsBuilder(Model model, PluginData data, List<Point> pathPoints)
        {
            _model = model;
            _data = data;
            _pathPoints = pathPoints;
        }

        public int Build()
        {
            if (_pathPoints.Count < 2) return 0;
            
            int postCount = 0;
            double startOffsetMm = _data.StartOffset;
            double endOffsetMm = _data.EndOffset;
            double lateralOffsetMm = _data.Offset;
            double spanMm = _data.Span;
            double baseHeightMm = _data.Height;

            if (Math.Abs(lateralOffsetMm) > 1e-6)
            {
                Vector overallDir = new Vector(_pathPoints[_pathPoints.Count - 1].X - _pathPoints[0].X, 
                                               _pathPoints[_pathPoints.Count - 1].Y - _pathPoints[0].Y, 0);
                if (overallDir.GetLength() > 1e-6)
                {
                    overallDir = overallDir.GetNormal();
                    Vector lateral = new Vector(-overallDir.Y, overallDir.X, 0).GetNormal();
                    
                    for (int i = 0; i < _pathPoints.Count; i++)
                    {
                        _pathPoints[i] = _pathPoints[i] + lateral * lateralOffsetMm;
                    }
                }
            }

            List<double> segmentLengths = new List<double>();
            double totalLength = 0;
            for (int i = 0; i < _pathPoints.Count - 1; i++)
            {
                double len = Tekla.Structures.Geometry3d.Distance.PointToPoint(_pathPoints[i], _pathPoints[i + 1]);
                segmentLengths.Add(len);
                totalLength += len;
            }

            if (totalLength < 1e-6) return 0;

            double usableLength = totalLength - startOffsetMm - endOffsetMm;
            List<double> stations = new List<double>();

            if (usableLength > 1e-3)
            {
                if (_data.SpaceScheme == 1)
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
                else
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
                
                double accumulated = 0;
                int segIndex = 0;
                double distInSeg = 0;
                
                for (int s = 0; s < segmentLengths.Count; s++)
                {
                    if (dist <= accumulated + segmentLengths[s] + 1e-3)
                    {
                        segIndex = s;
                        distInSeg = dist - accumulated;
                        if (distInSeg < 0) distInSeg = 0;
                        if (distInSeg > segmentLengths[s]) distInSeg = segmentLengths[s];
                        break;
                    }
                    accumulated += segmentLengths[s];
                    
                    if (s == segmentLengths.Count - 1)
                    {
                        segIndex = s;
                        distInSeg = segmentLengths[s];
                    }
                }

                Point segStart = _pathPoints[segIndex];
                Point segEnd = _pathPoints[segIndex + 1];
                Vector segDir = new Vector(segEnd.X - segStart.X, segEnd.Y - segStart.Y, segEnd.Z - segStart.Z);
                
                if (segDir.GetLength() > 1e-6)
                {
                    segDir = segDir.GetNormal();
                    Point basePt = segStart + segDir * distInSeg;
                    
                    double h = Math.Sqrt(segDir.X * segDir.X + segDir.Y * segDir.Y);
                    double currentHeight = baseHeightMm;

                    bool isLevel = h > 0.99;
                    bool isFirstSegment = segIndex == 0;
                    bool isLastSegment = segIndex == _pathPoints.Count - 2;

                    if (isLevel && isFirstSegment && _pathPoints.Count > 2)
                    {
                        if (_data.LevelStartMode == 1) // Independent
                        {
                            currentHeight = baseHeightMm + _data.LevelStartHeight;
                        }
                        else // Project
                        {
                            Vector stairDir = new Vector(_pathPoints[2].X - _pathPoints[1].X, _pathPoints[2].Y - _pathPoints[1].Y, _pathPoints[2].Z - _pathPoints[1].Z);
                            if (stairDir.GetLength() > 1e-6)
                            {
                                stairDir = stairDir.GetNormal();
                                double stairH = Math.Sqrt(stairDir.X * stairDir.X + stairDir.Y * stairDir.Y);
                                if (stairH > 1e-6)
                                {
                                    double verticalGap = _data.NosingOffset / stairH;
                                    double stairSlope = stairDir.Z / stairH;
                                    double xyDistFromStair = Math.Sqrt(Math.Pow(basePt.X - _pathPoints[1].X, 2) + Math.Pow(basePt.Y - _pathPoints[1].Y, 2));
                                    double targetTopZ = _pathPoints[1].Z + verticalGap + baseHeightMm - (xyDistFromStair * stairSlope);
                                    currentHeight = targetTopZ - basePt.Z;
                                }
                            }
                        }
                    }
                    else if (isLevel && isLastSegment && _pathPoints.Count > 2)
                    {
                        if (_data.LevelEndMode == 1) // Independent
                        {
                            currentHeight = baseHeightMm + _data.LevelEndHeight;
                        }
                        else // Project
                        {
                            Vector stairDir = new Vector(_pathPoints[segIndex].X - _pathPoints[segIndex-1].X, _pathPoints[segIndex].Y - _pathPoints[segIndex-1].Y, _pathPoints[segIndex].Z - _pathPoints[segIndex-1].Z);
                            if (stairDir.GetLength() > 1e-6)
                            {
                                stairDir = stairDir.GetNormal();
                                double stairH = Math.Sqrt(stairDir.X * stairDir.X + stairDir.Y * stairDir.Y);
                                if (stairH > 1e-6)
                                {
                                    double verticalGap = _data.NosingOffset / stairH;
                                    double stairSlope = stairDir.Z / stairH;
                                    double xyDistFromStair = Math.Sqrt(Math.Pow(basePt.X - _pathPoints[segIndex].X, 2) + Math.Pow(basePt.Y - _pathPoints[segIndex].Y, 2));
                                    double targetTopZ = _pathPoints[segIndex].Z + verticalGap + baseHeightMm + (xyDistFromStair * stairSlope);
                                    currentHeight = targetTopZ - basePt.Z;
                                }
                            }
                        }
                    }
                    else // Normal segment
                    {
                        if (h > 1e-6 && Math.Abs(_data.NosingOffset) > 1e-6)
                        {
                            currentHeight = baseHeightMm + (_data.NosingOffset / h);
                        }
                    }

                    bool isFirst = (i == 0);
                    bool isLast = (i == stations.Count - 1);
                    InsertPlumbPost(basePt, currentHeight, isFirst, isLast);
                    postCount++;
                }
            }
            
            return postCount;
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
                if (_data.LastProfileLink == 1)
                {
                    prof = _data.FirstProfile; mat = _data.FirstMaterial; cls = _data.ClassFirst; name = _data.FirstPartName;
                }
                else if (_data.LastProfileLink == 2)
                {
                    if (_data.MidProfileLink == 1)
                    {
                        prof = _data.FirstProfile; mat = _data.FirstMaterial; cls = _data.ClassFirst; name = _data.FirstPartName;
                    }
                    else
                    {
                        prof = _data.MidProfile; mat = _data.MidMaterial; cls = _data.ClassMid; name = _data.MidPartName;
                    }
                }
                else
                {
                    prof = _data.LastProfile; mat = _data.LastMaterial; cls = _data.ClassLast; name = _data.LastPartName;
                }
            }
            else
            {
                if (_data.MidProfileLink == 1)
                {
                    prof = _data.FirstProfile; mat = _data.FirstMaterial; cls = _data.ClassFirst; name = _data.FirstPartName;
                }
                else if (_data.MidProfileLink == 2)
                {
                    if (_data.LastProfileLink == 1)
                    {
                        prof = _data.FirstProfile; mat = _data.FirstMaterial; cls = _data.ClassFirst; name = _data.FirstPartName;
                    }
                    else
                    {
                        prof = _data.LastProfile; mat = _data.LastMaterial; cls = _data.ClassLast; name = _data.LastPartName;
                    }
                }
                else
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
