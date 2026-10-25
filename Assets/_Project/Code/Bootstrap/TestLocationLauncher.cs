using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Bootstrap
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Тест отдельной локации")]
    public sealed class TestLocationLauncher : MonoBehaviour
    {
        public const string BootstrapPath = "Assets/_Project/Scenes/TestBootstrap.unity";
        public const string HighwayPath = "Assets/_Project/Locations/Highway/Highway.unity";
        public const string HousePath = "Assets/_Project/Locations/House/House.unity";
        public const string CourtyardPath = "Assets/_Project/Locations/CourtyardIsland/CourtyardIsland.unity";

        public const string GaragePath = "Assets/_Project/Locations/Garage/Garage.unity";

        public const string SchoolPath = "Assets/_Project/Locations/School/School.unity";

        public enum Location
        {
            [InspectorName("Тестовая комната")] TestRoom,
            [InspectorName("Шоссе")] Highway,
            [InspectorName("Дом")] House,
            [InspectorName("Двор-колодец")] Courtyard,
            [InspectorName("Гараж")] Garage,
            [InspectorName("Школа")] School
        }

        [SerializeField] private Location location;
        public Location SelectedLocation { get => location; set => location = value; }
        public string ScenePath => location switch
        {
            Location.Highway => HighwayPath,
            Location.House => HousePath,
            Location.Courtyard => CourtyardPath,
            Location.Garage => GaragePath,
            Location.School => SchoolPath,
            _ => AppLifetimeScope.DefaultLocationPath
        };

        internal void Prepare(Scene scene)
        {
            if (scene.path != ScenePath) return;
            if (location == Location.Highway)
            {
                var boundary = new GameObject("Тестовая граница шоссе");
                SceneManager.MoveGameObjectToScene(boundary, scene);
                boundary.transform.position = new Vector3(0, 2.5f, 300.5f);
                boundary.AddComponent<BoxCollider>().size = new Vector3(13, 5, 1);
            }
            else if (location == Location.House)
            {
                var spawn = new GameObject("PlayerSpawn");
                SceneManager.MoveGameObjectToScene(spawn, scene);
                spawn.transform.position = new Vector3(0, 0.05f, 305);
                var view = new GameObject("Тестовая камера дома");
                SceneManager.MoveGameObjectToScene(view, scene);
                view.tag = "MainCamera";
                view.transform.position = spawn.transform.position + Vector3.up * 1.65f;
                var camera = view.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 1000;
                view.AddComponent<AudioListener>();
                // The path normally joins the highway; close its free edge in isolation.
                var boundary = new GameObject("Тестовая граница перед домом");
                SceneManager.MoveGameObjectToScene(boundary, scene);
                boundary.transform.position = new Vector3(0, 2.5f, 299.5f);
                boundary.AddComponent<BoxCollider>().size = new Vector3(10, 5, 1);
            }
        }
    }
}
