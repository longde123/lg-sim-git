/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using Simulator.Bridge.Data;
using UnityEngine;
using System.Text;

namespace Simulator.Bridge.Ros
{
    class PointCloudWriterRos2 : IWriter<PointCloudData>
    {
        Writer<Snowball.PointCloud2> OriginalWriter;

        byte[] Buffer;

        public PointCloudWriterRos2(Bridge bridge, string topic)
        {
            OriginalWriter = new Writer<Snowball.PointCloud2>(bridge, topic, true);
        }

        public void Write(PointCloudData data, Action completed)
        {
            int _point_step = 18;
            int _LaserCount = data.LaserCount;
            if (Buffer == null || Buffer.Length != data.Points.Length)
            {
                Buffer = new byte[_point_step * data.Points.Length];
            }
            else {
                Debug.Log($" Error: not able allocate buffer for Pointcloud msg data");
            }
            
            int count = 0;
            unsafe
            {
                fixed (byte* ptr = Buffer)
                {
                    int offset = 0;
                    for (int i = 0; i < data.Points.Length; i++)
                    {
                        var point = data.Points[i];

                        var pos = new UnityEngine.Vector3(point.x, point.y, point.z);
                        float intensity = point.w;

                        *(UnityEngine.Vector3*)(ptr + offset) = data.Transform.MultiplyPoint3x4(pos);
                        *(float*)(ptr + offset + 12) = (intensity * 255);

                        offset += _point_step;
                        count++;
                    }
                }
            }
            
            var msg = new Snowball.PointCloud2()
            {
                header = new Snowball.Header()
                {
                    stamp = new Snowball.Time(DateTime.Now),
                    frame_id = data.Frame,
                },
                height = (uint)(count / _LaserCount),
                width = (uint)_LaserCount,
                fields = new []
                {
                    new Snowball.PointField()
                    {
                        name = "x",
                        offset = 0,
                        datatype = 7,
                        count = 1,
                    },
                    new Snowball.PointField()
                    {
                        name = "y",
                        offset = 4,
                        datatype = 7,
                        count = 1,
                    },
                    new Snowball.PointField()
                    {
                        name = "z",
                        offset = 8,
                        datatype = 7,
                        count = 1,
                    },
                    new Snowball.PointField()
                    {
                        name = "intensity",
                        offset = 12,
                        datatype = 7,
                        count = 1,
                    },
                    new Snowball.PointField()
                    {
                        name = "ring",
                        offset = 16,
                        datatype = 4,
                        count = 1,
                    },
                },
                is_bigendian = false,
                point_step = (uint)_point_step,            // Size of one single laser point
                row_step = (uint)(_LaserCount * _point_step), // Size of one single row step = laser count * point setp
                data = Buffer,
                is_dense = true,
            };
            OriginalWriter.Write(msg, completed); 
            
        }
    }
}
