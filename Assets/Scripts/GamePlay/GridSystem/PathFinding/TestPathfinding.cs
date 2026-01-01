using UnityEngine;

namespace GridSystem.PathFinding
{
    public class TestPathfinding : MonoBehaviour
    {
        public GridNavigator navigator;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                navigator.MoveTo(this.transform.position);
            }
        }
    }
}