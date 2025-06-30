"""
Defines the manage_gameobject tool for GameObject management in Unity.
"""
from typing import Dict, Any, Optional, List
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_manage_gameobject_tools(mcp: FastMCP):
    """Registers the manage_gameobject tool with the MCP server."""

    @mcp.tool()
    def manage_gameobject(
        ctx: Context,
        action: str,
        target: Optional[str] = None,
        search_method: Optional[str] = None,
        name: Optional[str] = None,
        tag: Optional[str] = None,
        layer: Optional[str] = None,
        parent: Optional[str] = None,
        position: Optional[List[float]] = None,
        rotation: Optional[List[float]] = None,
        scale: Optional[List[float]] = None,
        primitive_type: Optional[str] = None,
        components_to_add: Optional[List[str]] = None,
        components_to_remove: Optional[List[str]] = None,
        component_properties: Optional[Dict[str, Any]] = None,
        set_active: Optional[bool] = None,
        save_as_prefab: Optional[bool] = None,
        prefab_path: Optional[str] = None,
    ) -> Dict[str, Any]:
        """Manages GameObjects: create, modify, delete, find, and component operations.

        Args:
            ctx: The MCP context.
            action: Operation (e.g., 'create', 'find', 'modify', 'delete').
            target: GameObject identifier (name or path) for modify/delete actions.
            search_method: How to find objects ('by_name', 'by_tag', 'by_path').
            name: Name for 'create' or new name for 'modify'.
            tag: Tag for 'create' or new tag for 'modify'.
            layer: Layer for 'create' or new layer for 'modify'.
            parent: Parent GameObject path for 'create' or 'modify'.
            position: Position for 'create' or 'modify'.
            rotation: Rotation (Euler angles) for 'create' or 'modify'.
            scale: Scale for 'create' or 'modify'.
            primitive_type: For 'create' action, specifies primitive type (e.g., 'Cube', 'Sphere').
            components_to_add: List of component names to add.
            components_to_remove: List of component names to remove.
            component_properties: Dictionary of component properties to set.
            set_active: Sets the active state of the GameObject.
            save_as_prefab: If true, saves the created/modified GameObject as a prefab.
            prefab_path: Path to save the prefab (e.g., "Assets/Prefabs/MyObject.prefab").

        Returns:
            Dictionary with operation results ('success', 'message', 'data').
        """
        bridge = get_unity_connection()

        params_dict = {
            "action": action.lower(),
            "target": target,
            "searchMethod": search_method,
            "name": name,
            "tag": tag,
            "layer": layer,
            "parent": parent,
            "position": position,
            "rotation": rotation,
            "scale": scale,
            "primitiveType": primitive_type,
            "componentsToAdd": components_to_add,
            "componentsToRemove": components_to_remove,
            "componentProperties": component_properties,
            "setActive": set_active,
            "saveAsPrefab": save_as_prefab,
            "prefabPath": prefab_path,
        }

        params_dict = {k: v for k, v in params_dict.items() if v is not None}

        return bridge.send_command("manage_gameobject", params_dict) 
