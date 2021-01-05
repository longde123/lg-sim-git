/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Plugins;
using Simulator.Utilities;
using Simulator.Sensors.UI;

namespace Simulator.Sensors
{
    [SensorType("Color Camera", new[] { typeof(ImageData) })]
    [RequireComponent(typeof(Camera))]
    public class ColorCameraSensor : SensorBase
    {
        [SensorParameter]
        [Range(1, 1920)]
        public int Width = 1920;

        [SensorParameter]
        [Range(1, 1080)]
        public int Height = 1080;

        [SensorParameter]
        [Range(1, 100)]
        public int Frequency = 15;

        [SensorParameter]
        [Range(0, 100)]
        public int JpegQuality = 75;

        [SensorParameter]
        [Range(1.0f, 90.0f)]
        public float FieldOfView = 60.0f;

        [SensorParameter]
        [Range(0.01f, 1000.0f)]
        public float MinDistance = 0.1f;

        [SensorParameter]
        [Range(0.01f, 2000.0f)]
        public float MaxDistance = 1000.0f;
        
        [SensorParameter]
        public float configTransformX;

        [SensorParameter]
        public float configTransformY;

        [SensorParameter]
        public float configTransformZ;

        [SensorParameter]
        public float configTransformPITCH;
        
        [SensorParameter]
        public float configTransformYAW;
        
        [SensorParameter]
        public float configTransformROLL;

        [SensorParameter]
        public List<double> d = new List<double>(new double[] { 0, 0, 0, 0, 0 });

        [SensorParameter]
        public List<double> k = new List<double>(new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 });

        [SensorParameter]
        public List<double> r = new List<double>(new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 });

        [SensorParameter]
        public List<double> p = new List<double>(new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0});

        IBridge Bridge;
        IWriter<ImageData> ImageWriter;

        IWriter<StaticTFData> StaticTFWriter;
        uint Sequence;

        const int MaxJpegSize = 4 * 1024 * 1024; // 4MB

        private Camera Camera;
        private float NextCaptureTime;

        Vector3 TFRranslation;
        Quaternion TFRotation;
        String frame_id;

        long lastTFTransmitInterval;

        IWriter<CameraInfoData> ImageCamInfoWriter;

        CameraInfoData CameraInfoDataBuf;

	byte[] compBuffer;

        private struct CameraCapture
        {
            public AsyncGPUReadbackRequest Request;
            public double CaptureTime;
        }

        private Queue<CameraCapture> CaptureQueue = new Queue<CameraCapture>();
        private ConcurrentBag<byte[]> JpegOutput = new ConcurrentBag<byte[]>();

        public void Start()
        {
            Camera = GetComponent<Camera>();
            Camera.enabled = false;
            lastTFTransmitInterval = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public void OnDestroy()
        {
            Camera.targetTexture?.Release();
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            string[] values = Topic.Split('/');
            string camInfoTopic = "";
            
            // Ex Topic: /camera/long_camera/compressed or /camera/long_camera/raw or /camera/long_camera
            if (values[(values.Length - 1)].ToLower() == "compressed" || values[(values.Length - 1)].ToLower() == "raw")
            {
                //frame_id = values[values.Length - 2];
                frame_id = Frame;
                for (int i = 0; i < values.Length - 1; i++)
                {
                    camInfoTopic = camInfoTopic + values[i].ToString() + "/";
                }
                camInfoTopic = camInfoTopic + "camera_info";
            }
            else
            {
                //frame_id = values[values.Length - 1];
                frame_id = Frame;
                camInfoTopic = Topic + "/camera_info";
                Topic = Topic + "/compressed"; // Mentioning that, topic contains compressed image.      
            }

            Bridge = bridge;
            ImageWriter = bridge.AddWriter<ImageData>(Topic);
            StaticTFWriter = bridge.AddWriter<StaticTFData>("/tf_static");
            ImageCamInfoWriter = bridge.AddWriter<CameraInfoData>(camInfoTopic);

            Vector3 eur = new Vector3(configTransformPITCH, configTransformYAW, configTransformROLL);
            TFRranslation = new Vector3(configTransformX, configTransformY, configTransformZ);
            TFRotation = Quaternion.Euler(eur);
        }

        public void Update()
        {

            long interval = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            if ((interval - lastTFTransmitInterval) > 3000) // 3 Sec
            {
                lastTFTransmitInterval = interval;
                StaticTFWriter.Write(new StaticTFData()
                {
                    Time = Time.time,
                    frame_id = frame_id,
                    child_frame_id = "novatel",
                    translation = TFRranslation,
                    rotation = TFRotation,
                });
            }

            Camera.fieldOfView = FieldOfView;
            Camera.nearClipPlane = MinDistance;
            Camera.farClipPlane = MaxDistance;

            CheckTexture();
            CheckCapture();
            ProcessReadbackRequests();
        }

        void CheckTexture()
        {
            // if this is not first time
            if (Camera.targetTexture != null)
            {
                if (Width != Camera.targetTexture.width || Height != Camera.targetTexture.height)
                {
                    // if camera capture size has changed
                    Camera.targetTexture.Release();
                    Camera.targetTexture = null;
                }
                else if (!Camera.targetTexture.IsCreated())
                {
                    // if we have lost rendertexture due to Unity window resizing or otherwise
                    Camera.targetTexture.Release();
                    Camera.targetTexture = null;
                }
            }

            if (Camera.targetTexture == null)
            {
                Camera.targetTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                {
                    dimension = TextureDimension.Tex2D,
                    antiAliasing = 1,
                    useMipMap = false,
                    useDynamicScale = false,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
            }
        }

        void CheckCapture()
        {
            if (Time.time >= NextCaptureTime)
            {
                Camera.Render();

                var capture = new CameraCapture()
                {
                    CaptureTime = SimulatorManager.Instance.CurrentTime,
                    Request = AsyncGPUReadback.Request(Camera.targetTexture, 0, TextureFormat.RGBA32),
                };
                CaptureQueue.Enqueue(capture);

                NextCaptureTime = Time.time + (1.0f / Frequency);
            }
        }

        void ProcessReadbackRequests()
        {
            while (CaptureQueue.Count > 0)
            {
                var capture = CaptureQueue.Peek();
                if (capture.Request.hasError)
                {
                    CaptureQueue.Dequeue();
                    Debug.Log("Failed to read GPU texture");
                }
                else if (capture.Request.done)
                {
                    CaptureQueue.Dequeue();
                    var data = capture.Request.GetData<byte>();

                    var imageData = new ImageData()
                    {
                        Name = Name,
                        Frame = Frame,
                        Width = Width,
                        Height = Height,
                        Sequence = Sequence,
                    };

                    if (!JpegOutput.TryTake(out compBuffer))
                    {
                        compBuffer = new byte[MaxJpegSize];
                    }

                    if (Bridge != null && Bridge.Status == Status.Connected)
                    {

                        ImageCamInfoWriter.Write(new CameraInfoData()
                        {
                            Time = Time.time,
                            frame_id = frame_id,
                            Width = Width,
                            Height = Height,
                            d = d.ToArray(),
                            k = k.ToArray(),
                            r = r.ToArray(),
                            p = p.ToArray(),
                        });
                        Task.Run(() =>
                        {
                            imageData.Length = JpegEncoder.Encode(data, Width, Height, 4, JpegQuality, compBuffer);
                            if (imageData.Length > 0)
                            {
                                imageData.Bytes = new byte[imageData.Length];

                                if (imageData.Bytes == null)
                                {
                                    Debug.Log("Buffer Is empty");
                                    imageData.Bytes = new byte[imageData.Length];
                                }

                                Buffer.BlockCopy(compBuffer, 0, imageData.Bytes, 0, imageData.Length);

                                imageData.Time = capture.CaptureTime;
                                ImageWriter.Write(imageData);

                                JpegOutput.Add(compBuffer);
                                
                            }
                            else
                            {
                                Debug.Log("Compressed image is empty, length = 0");
                            }
                        });
                    }

                    Sequence++;
                }
                else
                {
                    break;
                }
            }
        }

        public bool Save(string path, int quality, int compression)
        {
            CheckTexture();
            Camera.Render();
            var readback = AsyncGPUReadback.Request(Camera.targetTexture, 0, TextureFormat.RGBA32);
            readback.WaitForCompletion();

            if (readback.hasError)
            {
                Debug.Log("Failed to read GPU texture");
                return false;
            }

            Debug.Assert(readback.done);
            var data = readback.GetData<byte>();

            var bytes = new byte[16 * 1024 * 1024];
            int length;

            var ext = System.IO.Path.GetExtension(path).ToLower();

            if (ext == ".png")
            {
                length = PngEncoder.Encode(data, Width, Height, 4, compression, bytes);
            }
            else if (ext == ".jpeg" || ext == ".jpg")
            {
                length = JpegEncoder.Encode(data, Width, Height, 4, quality, bytes);
            }
            else
            {
                return false;
            }

            if (length > 0)
            {
                try
                {
                    using (var file = System.IO.File.Create(path))
                    {
                        file.Write(bytes, 0, length);
                    }
                    return true;
                }
                catch
                {
                }
            }

            return false;
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);
            visualizer.UpdateRenderTexture(Camera.activeTexture, Camera.aspect);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
