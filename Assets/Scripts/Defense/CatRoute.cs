using UnityEngine;

namespace BulkoGame.Defense
{
    /// <summary>Scene-authored waypoint data for one entrance-to-basket route.</summary>
    public sealed class CatRoute : MonoBehaviour
    {
        [SerializeField] private string routeId;
        [SerializeField] private Transform[] waypoints;

        public string RouteId => routeId;
        public int Count => waypoints == null ? 0 : waypoints.Length;
        public Vector2 Entrance => Count > 0 ? waypoints[0].position : transform.position;
        public Vector2 BasketApproach => Count > 0 ? waypoints[Count - 1].position : transform.position;

        public Vector2 Point(int index) => waypoints[index].position;

        public void Configure(Transform[] points)
        {
            waypoints = points;
        }
    }
}
