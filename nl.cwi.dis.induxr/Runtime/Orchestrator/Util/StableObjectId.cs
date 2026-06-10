using UnityEngine;

namespace Orchestrator.Util
{
    public static class StableObjectId
    {
        /// <summary>
        /// Generates a unique and stable identifier for a GameObject by constructing its hierarchical path in the scene
        /// and appending the scene name to ensure uniqueness across different scenes.
        /// </summary>
        /// <param name="obj">The GameObject for which the stable ID is to be generated.</param>
        /// <returns>A string representing the stable and unique identifier of the GameObject based on its hierarchy and scene context.</returns>
        public static string GetSceneObjectId(GameObject obj)
        {
            // Build the hierarchy path (e.g. "Environment/Rooms/Table_3")
            string path = obj.name;
            Transform parent = obj.transform.parent;

            // Keep attaching parent names until we reach the root of the hierarchy
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            // Add scene name to prevent collisions
            path = obj.scene.name + "/" + path;

            // Hash path and return as string
            return GetStableHash32(path).ToString();
        }

        /// <summary>
        /// Computes a 32-bit stable hash for the given string input using the FNV-1a hash algorithm.
        /// The hash remains consistent across different runs of the application for the same string input.
        /// </summary>
        /// <param name="text">The input string for which the stable hash is to be computed.</param>
        /// <returns>A 32-bit unsigned integer representing the computed hash value.</returns>
        private static uint GetStableHash32(string text)
        {
            unchecked
            {
                // Initialise hash to FNV offset basis
                var hash = 2166136261;

                foreach (var c in text)
                {
                    // XOR hash with current character and multiply with FNV prime
                    hash = (hash ^ c) * 16777619;
                }

                return hash;
            }
        }
    }
}
