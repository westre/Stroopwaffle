﻿namespace Stroopwaffle_Server {
    public class Vector3 {

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString() {
            return "[" + X + ", " + Y + ", " + Z + "]";
        }
    }
}