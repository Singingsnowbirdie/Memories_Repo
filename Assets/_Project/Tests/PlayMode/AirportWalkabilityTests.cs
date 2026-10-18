using System.Collections;
using System.Linq;
using Memories.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Memories.Tests.PlayMode
{
    public sealed class AirportWalkabilityTests
    {
        private const string ScenePath = "Assets/_Project/Locations/AirportIsland/AirportIsland.unity";
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        [UnityTest]
        public IEnumerator AllIslandConnectionsAreWalkableWithoutJumping()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var actor = new GameObject("Walkability test player");
            SceneManager.MoveGameObjectToScene(actor, scene);
            var motor = actor.AddComponent<PlayerMotor>();
            var camera = new GameObject("Walkability test view").transform;
            motor.Initialize(new PlayerMovementSettings(), camera);
            var controller = actor.GetComponent<CharacterController>();
            try
            {
                var routeRoot = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>())
                    .Single(t => t.name == "Safe connecting routes");
                Assert.AreEqual(6, routeRoot.childCount);
                foreach (Transform route in routeRoot)
                {
                    var a = route.Find("Route start").position;
                    var b = route.Find("Route end").position;
                    var direction = b - a;
                    direction.y = 0;
                    direction.Normalize();
                    yield return Walk(motor, controller, new[] { a - direction * 1.5f, a, b, b + direction * 1.5f }, route.name);
                    yield return Walk(motor, controller, new[] { b + direction * 1.5f, b, a, a - direction * 1.5f }, route.name + " reverse");
                    var midpoint = (a + b) * .5f;
                    yield return Walk(motor, controller, new[] { a, midpoint }, route.name + " stop on slope");
                    var restingPosition = motor.transform.position;
                    for (int i = 0; i < 120; i++) motor.Move(Vector3.down * .04f);
                    Assert.Less(Vector3.Distance(restingPosition, motor.transform.position), .15f, route.name + " slides while idle");
                    var side = Vector3.Cross(Vector3.up, direction);
                    for (int i = 0; i < 100; i++) motor.Move(side * .08f + Vector3.down * .04f);
                    Assert.Less(Mathf.Abs(Vector3.Dot(motor.transform.position - midpoint, side)), 2.4f, route.name + " side protection failed");
                    Assert.Greater(motor.transform.position.y, midpoint.y - .5f, route.name + " fell off side");
                }
                yield return Walk(motor, controller, new[] { V(-27,0,-62),V(-25,0,-62),V(-25,0,-46) }, "Spawn to stairs");
                yield return Walk(motor, controller, new[] { V(-25,4,-24),V(-25,4,-11),V(-25,4,1) }, "Through terminal");
                yield return Walk(motor, controller, new[] { V(-10,8,23),V(-10,8,34),V(-17,8,34) }, "Apron to hangar bridge");
                yield return Walk(motor, controller, new[] { V(-42,14,34),V(-58,14,34),V(-58,14,48) }, "Into hangar");
                yield return Walk(motor, controller, new[] { V(-10,8,23),V(-10,8,50),V(25,8,50) }, "Apron to runway bridge");
                yield return Walk(motor, controller, new[] { V(60,18,50),V(70,18,50),V(70,18,95) }, "Along runway");
                yield return Walk(motor, controller, new[] { V(-1,5,-43),V(-1,5,-34),V(5,5,-34) }, "Around sculpture garden");
            }
            finally
            {
                Object.Destroy(actor);
            }
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTearDown]
        public IEnumerator CleanupScene()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static IEnumerator Walk(PlayerMotor motor, CharacterController controller, Vector3[] points, string name)
        {
            controller.enabled = false;
            motor.transform.position = points[0] + Vector3.up * .15f;
            controller.enabled = true;
            Physics.SyncTransforms();
            for (int i = 0; i < 20; i++) motor.Move(Vector3.down * .05f);
            for (int segment = 1; segment < points.Length; segment++)
            {
                var goal = points[segment];
                bool reached = false;
                for (int step = 0; step < 1800; step++)
                {
                    var delta = goal - motor.transform.position;
                    delta.y = 0;
                    if (delta.magnitude < .12f) { reached = true; break; }
                    motor.Move(Vector3.ClampMagnitude(delta, 4f / 60f) + Vector3.down * .04f);
                    Assert.Greater(motor.transform.position.y, Mathf.Min(points[segment-1].y, goal.y) - .5f, name + " fell below route");
                    if (step % 20 == 0) yield return null;
                }
                Assert.IsTrue(reached, name + " blocked at " + motor.transform.position + " heading to " + goal);
                Assert.That(motor.transform.position.y, Is.EqualTo(goal.y).Within(.35f), name + " endpoint elevation");
            }
        }
    }
}
