﻿using System.Collections.Generic;
using UnityEngine;

public class PlayerSelect : MonoBehaviour
{
    // Data
    private PlayerData m_Data = null;

    // Selection
    [SerializeField] private GameObject m_SelectionBoxPrefab = null;
    private SelectionBox m_SelectionBox = null;
    private bool m_Selecting = false;
    private bool m_Adding = false;

    // Grouping
    [SerializeField] private GameObject m_GroupLeaderPrefab = null;

    private void Start()
    {
        m_Data = GetComponent<PlayerData>();
    }

    private void Update()
    {
        // Get left mouse button input
        if (Input.GetMouseButton(0))
        {
            ClickDragSelect();
        }
        else
        {
            StopSelecting();
        }

        // Upon pressing escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearSelection();
        }

        // Upon pressing G
        if (Input.GetKeyDown(KeyCode.G))
        {
            // Check if we are holding down any of the shift keys
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                // Ungroup current selection
                UngroupSelection();
            }
            else
            {
                // Group current selection
                GroupSelection();
            }
        }
    }

    // Handle mouse click and drag select
    private void ClickDragSelect()
    {
        // See if we have a selection box present
        if (m_SelectionBox == null)
        {
            // Check if we are adding to our current selection
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                m_Adding = true;
            }

            // If no selectionbox exists, create one
            GameObject go = Instantiate(m_SelectionBoxPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
            m_SelectionBox = go.GetComponent<SelectionBox>();

            // Get mouse location
            Vector3 mouseLocation = Input.mousePosition;
            mouseLocation.z = Camera.main.transform.position.y;

            // Convert mouse location to world space
            Vector3 worldLocation = Camera.main.ScreenToWorldPoint(mouseLocation);

            // Set selectionbox boundaries to mouse position
            m_SelectionBox.SetStartLocation(worldLocation);
            m_SelectionBox.SetEndLocation(worldLocation);
        }
        else
        {
            // Check if player hasn't released control
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                m_Adding = false;
            }

            // Get mouse location
            Vector3 mouseLocation = Input.mousePosition;
            mouseLocation.z = Camera.main.transform.position.y;

            // Convert mouse location to world space
            Vector3 worldLocation = Camera.main.ScreenToWorldPoint(mouseLocation);

            // Update end location, which will update selectionbox boundaries
            m_SelectionBox.SetEndLocation(worldLocation);
        }

        // Set selecting to true (holding down left click)
        m_Selecting = true;
    }

    private void StopSelecting()
    {
        // Only execute when selecting
        if (!m_Selecting)
            return;

        // If not adding to our current selection, clear it
        if (!m_Adding)
        {
            ClearSelection();
        }

        // Select units, and stop selecting
        SelectUnits();
        m_Selecting = false;
        m_Adding = false;
    }

    // Select units inside selection box
    private void SelectUnits()
    {
        // Only execute if we have a box reference
        if (m_SelectionBox == null)
            return;

        // Get all overlapping objects
        List<GameObject> overlappingObjects = m_SelectionBox.GetOverlappingObjects();

        // Loop over all objects
        foreach (GameObject go in overlappingObjects)
        {
            // Check if gameobject they have behavior script
            UnitBehavior unit = go.GetComponent<UnitBehavior>();
            if (unit == null)
                continue;

            // If we are adding to our selection
            if (m_Adding)
            {
                // If duplicate, don't add this unit
                if (m_Data.selectedUnits.Contains(unit))
                    continue;
            }

            // Get the unit's leader
            GroupLeader leader = unit.GetLeader();

            // If unit doesn't have a leader
            if (leader == null)
            {
                // Add unit
                m_Data.selectedUnits.Add(unit);
                unit.Select();
            }
            else
            {
                // If duplicate, don't add this leader
                if (m_Data.selectedLeaders.Contains(leader))
                    continue;

                // Loop over all units in the leader's group and select them
                foreach (UnitBehavior groupUnit in leader.units)
                {
                    groupUnit.Select();
                }

                // Add leader to list
                m_Data.selectedLeaders.Add(leader);
            }
        }

        // Destroy the box and reset reference
        Destroy(m_SelectionBox.gameObject);
        m_SelectionBox = null;
    }

    // Clear our selected units
    private void ClearSelection()
    {
        // Only execute if we have units
        if (m_Data.selectedUnits.Count == 0 && m_Data.selectedLeaders.Count == 0)
            return;

        // Loop over all units
        foreach (UnitBehavior unit in m_Data.selectedUnits)
        {
            unit.Deselect();
        }

        // Clear the list of units
        m_Data.selectedUnits.Clear();

        // Loop over all leaders
        foreach (GroupLeader leader in m_Data.selectedLeaders)
        {
            // Loop over all units in group
            foreach (UnitBehavior unit in leader.units)
            {
                unit.Deselect();
            }
        }

        // Clear the list of leaders
        m_Data.selectedLeaders.Clear();
    }

    // Group selected units
    private void GroupSelection()
    {
        // Only execute if we have selection
        if (m_Data.selectedUnits.Count == 0 && m_Data.selectedLeaders.Count == 0)
            return;

        // Ungroup selection before regrouping
        UngroupSelection();

        // Get the average location of selected units
        Vector3 averageLocation = new Vector3();
        // Loop over all units
        foreach (UnitBehavior unit in m_Data.selectedUnits)
        {
            averageLocation += unit.transform.position;
        }
        averageLocation /= m_Data.selectedUnits.Count;

        // Instantiate a leader in middle
        GameObject go = Instantiate(m_GroupLeaderPrefab, averageLocation, Quaternion.identity);
        GroupLeader leader = go.GetComponent<GroupLeader>();

        // Loop over all units
        foreach (UnitBehavior unit in m_Data.selectedUnits)
        {
            // Add to leader list of units and set unit leader
            leader.units.Add(unit);
            unit.SetLeader(leader);
        }

        // Clear list of selected units
        m_Data.selectedUnits.Clear();

        // Add leader to selected leaders
        m_Data.selectedLeaders.Add(leader);
    }

    // Ungroup selected units
    private void UngroupSelection()
    {
        // Only execute if we have selection
        if (m_Data.selectedUnits.Count == 0 && m_Data.selectedLeaders.Count == 0)
            return;

        // Loop over all selected leaders
        foreach (GroupLeader selectedLeader in m_Data.selectedLeaders)
        {
            // Loop over all units in group
            foreach (UnitBehavior unit in selectedLeader.units)
            {
                // Add them to our current selected units
                m_Data.selectedUnits.Add(unit);
            }

            // Destroy selected leader
            Destroy(selectedLeader.gameObject);
        }

        // Clear the list of leaders
        m_Data.selectedLeaders.Clear();
    }
}
