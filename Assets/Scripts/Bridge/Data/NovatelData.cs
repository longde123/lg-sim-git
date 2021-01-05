/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class novatelInsPvaData
    {
        public DateTime stamp;
        public string frame_id;

        public int week;
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
        public int status;
    }

    public class novatelRawImuXData
    {
        public DateTime stamp;
        public string frame_id;

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

    public class novatelMarkCountData
    {
        public DateTime stamp;
        public string frame_id;

        public UInt32 mark_num;
        public UInt32 period;
        public UInt16 count;
    }

    public class novatelBestPosData
    {
        public DateTime stamp;
        public string frame_id;

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
    public class novatelCorrIMUData
    {
        public DateTime stamp;
        public string frame_id;
        public UInt32 week;
        public float seconds;
        public float pitch_rate;
        public float roll_rate;
        public float yaw_rate;
        public float lateral_acc;
        public float longitudinal_acc;
        public float vertical_acc;
    }

    public class novatelHeadingData
    {
        public DateTime stamp;
        public string frame_id;
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
}
