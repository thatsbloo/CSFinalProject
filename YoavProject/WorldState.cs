using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace YoavProject
{
    public class WorldState
    {
        private Dictionary<int, InteractableObject> interactables;
        public enum InteractableTypes: byte { Table = 0, Workstation = 1}

        public WorldState()
        {
            this.interactables = new Dictionary<int, InteractableObject>();
        }

        public void addWorldInteractable(int id, InteractableObject inter)
        {
            interactables.Add(id, inter);
        }

        public byte[] getWorldState()
        {
            List<byte> result = new List<byte>();
            foreach (var pair in interactables)
            {
                result.Add((byte)pair.Key);
                Console.WriteLine((byte)pair.Key);
                result.AddRange(pair.Value.getByteData());
            }
            return result.ToArray();
        }

        public void clearWorldMap()
        {
            interactables.Clear();
        }

        public void updateInteractable(int id, InteractableObject newVersion)
        {
            interactables[id] = newVersion; 
        }

        public int getInteractableCount()
        {
            return interactables.Count; 
        }

        public bool interactWith(int id)
        {
            if (interactables.ContainsKey(id))
            {
                return interactables[id].interact();
            }
            return false;
        }

        public bool canInteractWith(int id)
        {
            if (interactables.ContainsKey(id))
            {
                return true;
            }
            return false;
        }

        public void setHighlight(bool highlight, int id)
        {
            if (interactables.ContainsKey(id))
            {
                interactables[id].highlighted = highlight;
            }
        }

        public List<InteractableObject> getInteractableObjects()
        {
            return interactables.Values.ToList();
        }

        public Dictionary<int, InteractableObject> getObjectsDictionary()
        {
            return new Dictionary<int, InteractableObject>(interactables); // Caller gets a copy
        }

        public void setUpForGameMap()
        {
            clearWorldMap();
            addWorldInteractable(1, new Workstation(new PointF(0, 3)));
            addWorldInteractable(2, new Workstation(new PointF(0, 9)));
            addWorldInteractable(3, new Workstation(new PointF(11, 3)));
            addWorldInteractable(4, new Workstation(new PointF(11, 9)));
        }

        public InteractableObject getInteractableObject(int id)
        {
            return interactables[id];
        }
    }
}
