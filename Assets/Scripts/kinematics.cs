using UnityEngine;
using System.Collections;

namespace Kinematics {
    public static class FreeFall {
        public static float MetersPerSecond(float gravity, float time) {
            return gravity * time;
        }

        public static float Velocity(float startVel, float gravity, float time) {
            return startVel - gravity * time;
        }

        public static float EndYposition(float startVel, float gravity, float time) {
            return startVel * time - .5f * gravity * Mathf.Pow(time, 2);
        }

        public static float FlightTime(float height, float gravity) {
            return Mathf.Sqrt((2 * height) / gravity);
        }
    }

    public static class HorizontalLaunch {
        public static float FlightTime(float angle, float velocity, float gravity) {
            return (2 * velocity * Mathf.Sin(angle)) / gravity;
        }

        public static float MaxHeight(float angle, float velocity, float gravity) {
            return ((velocity * velocity) * Mathf.Sin(angle * angle) / (2 * gravity));
        }

        public static float EndVelocity(float height, float gravity) {
            return Mathf.Sqrt(2 * gravity * height);
        }

        public static float StartVelocity(float range, float height, float gravity) {
            return range * Mathf.Sqrt(gravity / (2 * height));
        }

        public static float Height(float gravity, float time) {
            return 0.5f * gravity * Mathf.Pow(time, 2);
        }
    }
}
