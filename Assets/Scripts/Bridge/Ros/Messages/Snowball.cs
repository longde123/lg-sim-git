using System.Collections.Generic;
using System;

namespace Simulator.Bridge.Ros.Snowball
{
    [MessageType("builtin_interfaces/Time")]
    public class Time
    {
        public Int32 sec;
        public UInt32 nanosec;

        // Default Constructor
        public Time() {}

        // Convert from DateTime
        // Note that DateTime lowest resolution is in 100ns, so precision will be lost. 
        public Time(DateTime date)
        {
            double unixtime = (date.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            sec = (Int32)Math.Truncate(unixtime);
            nanosec = (UInt32)Math.Truncate((unixtime - sec) * Math.Pow(10, 9));
        }

        // Convert to DateTime
        public DateTime ToDateTime()
        {
            DateTime date = new DateTime(1970, 1, 1);
            date = date.AddSeconds(sec);

            double nano = (double)nanosec / Math.Pow(10, 9);
            date = date.AddSeconds(nano);

            return date;
        }
        public static implicit operator DateTime(Time t) => t.ToDateTime();

        public Time(double unixEpochSeconds)
        {
            long nanosec = (long)(unixEpochSeconds * 1e9);

            sec = (Int32)nanosec / 1000000000;
            nanosec = (UInt32)(nanosec % 1000000000);
        }
    }

    [MessageType("std_msgs/Header")]
    public class Header
    {
        public Time stamp;
        public string frame_id;
    }

    [MessageType("sensor_msgs/RegionOfInterest")]
    public class RegionOfInterest
    {
        public UInt32 x_offset;
        public UInt32 y_offset;
        public UInt32 height;
        public UInt32 width;
        public bool do_rectify;

    }
    [MessageType("novatel_msgs/InsPVA")]
    public class novatelInsPVA
    {
        public Header header;

        public Int32 week;
        public double seconds;
        public double latitude;
        public double longitude;
        public double height;
        public double north_velocity;
        public double east_velocity;
        public double up_velocity;
        public double roll;
        public double pitch;
        public double azimuth;
        public Int32 status;
    }

    [MessageType("novatel_msgs/RawImuX")]
    public class novatelRawImuX
    {
        public Header header;

        public byte error;
        public byte type;
        public UInt16 week;
        public double seconds;
        public Int32 status;
        public Int32 z_accel;
        public Int32 y_accel_neg;
        public Int32 x_accel;
        public Int32 z_gyro;
        public Int32 y_gyro_neg;
        public Int32 x_gyro;
    }

    [MessageType("novatel_msgs/MarkCount")]
    public class novatelMarkCount
    {
        public Header header;
        public UInt32 mark_num;
        public UInt32 period;
        public UInt16 count;
    }

    [MessageType("novatel_msgs/BestPos")]
    public class novatelBestPos
    {
        public Header header;
        public UInt32 sol_status;
        public string sol_status_str;
        public UInt32 pos_type;
        public string pos_type_str;
        public double lat;
        public double lon;
        public double hgt;
        public float undulation;
        public UInt32 datum_id;
        public float lat_std_dev;
        public float lon_std_dev;
        public float hgt_std_dev;
        public string std_id;
        public float diff_age;
        public float sol_age;
        public byte num_svs;
        public byte num_sol_in_svs;
        public byte num_sol_in_l1_svs;
        public byte num_sol_in_multi_svs;
        public byte ext_sol_stat;
        public byte galileo_beidou_sig_mask;
        public byte gps_glonass_sig_mask;
    }

    [MessageType("ipc_msgs/ControlData")]
    public class ControlData
    {
        public Header header;
        public double acceleration;
        public double steering_angle;
        public double time;
        public int mode;
    }

    [MessageType("ipc_msgs/ChassisData")]
    public class ChassisDataMsg
    {
        public Header header;
        public float steering_torque;
        public float engine_rpm;
        public float vehicle_speed;
        public float throttle_percent;
        public float brake_percent;
        public float steering_angle;
        public bool brake_status;
        public bool cruise_start;
        public bool cruise_cancel;
        public bool takeover;
    }
    [MessageType("novatel_msgs/CorrImuData")]
    public class novatelCorrIMU
    {
        public Header header;
        public UInt32 week;
        public float seconds;
        public float pitch_rate;
        public float roll_rate;
        public float yaw_rate;
        public float lateral_acc;
        public float longitudinal_acc;
        public float vertical_acc;
    }
    [MessageType("novatel_msgs/Heading")]
    public class novatelHeading
    {
    public Header header;
    public UInt32 sol_status;
    public string sol_status_str;
    public UInt32 pos_type;
    public string pos_type_str;
    public float length;
    public float heading;
    public float pitch;
    public float heading_std_dev;
    public float pitch_std_dev;
    public string station_id;
    public UInt32 num_svs;
    public UInt32 num_sol_in_svs;
    public UInt32 num_obs;
    public UInt32 num_multi;
    public UInt32 sol_source;
    public UInt32 ext_sol_stat;
    public UInt32 galileo_beidou_sig_mask;
    public UInt32 gps_glonass_sig_mask;
    }

    [MessageType("sensor_msgs/PointCloud2")]
    public class PointCloud2
    {
        public Header header;
        public uint height;
        public uint width;
        public PointField[] fields;
        public bool is_bigendian;
        public uint point_step;
        public uint row_step;
        public byte[] data;
        public bool is_dense;
    }

    [MessageType("sensor_msgs/PointField")]
    public class PointField
    {
        public const byte INT8 = 1;
        public const byte UINT8 = 2;
        public const byte INT16 = 3;
        public const byte UINT16 = 4;
        public const byte INT32 = 5;
        public const byte UINT32 = 6;
        public const byte FLOAT32 = 7;
        public const byte FLOAT64 = 8;

        public string name;
        public uint offset;
        public byte datatype;
        public uint count;
    }
	
	[MessageType("trackedobj_msgs/TrackedObject")]
    public class TrackedObject
    {
        public UInt32 track_id;
        public string obj_class;
        public float exist_conf;
        public float class_conf;
        public Pose pose;
        public Vector3 size;
        public Twist vel;
    }

    [MessageType("trackedobj_msgs/TrackedObjectArray")]
    public class TrackedObjectArray
    {
        public Header header;
        public List<TrackedObject> objects;
    }
	
    [MessageType("lidar_detection_msgs/DetectedObject")]
    public class DetectedObject
    {
        public Header header;
        public UInt32 id;
        public string label;
        public float score;
        public bool valid;
        public float yaw;
        public string space_frame;
        public Pose pose;
        public Vector3 dimensions;
        public Vector3 variance;
        public Twist velocity;
        public Twist acceleration;
        public PointCloud2 pointcloud;
        public bool pose_reliable;
        public bool velocity_reliable;
        public bool acceleration_reliable;
        public double x_pos;
        public double y_pos;
        public double z_pos;
        public double length_dim;
        public double width_dim;
        public double height_dim;
        public string image_frame;
        public int x;
        public int y;
        public int width;
        public int height;
        public float angle;
        public byte indicator_state;
        public byte behavior_state;
        public string[] user_defined_info;
    }

    [MessageType("lidar_detection_msgs/DetectedObjectArray")]
    public class DetectedObjectArray
    {
        public Header header;
        public List<DetectedObject> objects;
        public Image frame;
    }

    [MessageType("prediction_msgs/PerceptionObstacle")]
    public class Obstacle
    {
        public Header header;
        public UInt32 id;
        public string label;
        public float  score; 
        public Pose pose;
        public Vector3 dimensions;
        public Twist velocity;
        public Twist acceleration;

    }

  [MessageType("prediction_msgs/PerceptionObstaclesList")]
    public class ObstacleArray
    {
        public Header header;
        public List<Obstacle> obstacles;

    }

    [MessageType("sensor_msgs/CompressedImage")]
    public class CompressedImage
    {
        public Header header;
        public string format;
        public byte[] data;
    }

    [MessageType("sensor_msgs/CameraInfo")]
    public class CameraInfo
    {
        public Header header;
        public int height;
        public int width;
        // For "plumb_bob", the 5 parameters are: (k1, k2, t1, t2, k3).
        public double[] d;
        // Intrinsic camera matrix for the raw (distorted) images.
        public double[] k; // 3x3 row-major matrix
        // Rectification matrix (stereo cameras only)
        public double[] r; // 3x3 row-major matrix
        // Projection/camera matrix
        public double[] p; //3x4 row-major matrix
    }

    [MessageType("tf2_msgs/TFMessage")]
    public class tfMessage
    {    
        public TransformStamped[] transforms;
    }
   

    [MessageType("geometry_msgs/TransformStamped")]
    public class TransformStamped
    {
        public Header header;
        public string child_frame_id;
        public Transform transform;
    }

    [MessageType("geometry_msgs/Transform")]
    public class Transform
    {
        public Vector3 translation;
        public Quaternion rotation;
    }

    [MessageType("traffic_light_msgs/TrafficLightData")]
    public class TrafficLightData
    {
        public Header header;                              // Header timestamp should be acquisition time of image detection published                       
        public string trafficlight_id;                     // Id of the detected trafficlight
        public RegionOfInterest trafficlight_roi;          // Bounding box information of detected traffic light
        public byte color;                                 // Color information of detected traffic light   
        public const byte UNKNOWN_COLOR = 0;
        public const byte GREEN = 1;
        public const byte RED = 2;
        public const byte YELLOW = 3;
        public const byte BLACK = 4;

    }

}
