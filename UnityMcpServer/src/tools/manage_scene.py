"""
Defines the manage_scene tool for scene management in Unity.
"""
from typing import Dict, Any, Optional
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_manage_scene_tools(mcp: FastMCP):
    """Registers the manage_scene tool with the MCP server."""

    @mcp.tool()
    def manage_scene(
        ctx: Context,
        action: str,
        name: Optional[str] = None,
        path: Optional[str] = None,
        build_index: Optional[int] = None
    ) -> Dict[str, Any]:
        """Manages Unity scenes (load, save, create, get hierarchy, etc.).

        Args:
            ctx: The MCP context.
            action: Operation (e.g., 'load', 'save', 'create', 'get_hierarchy').
            name: Scene name (no extension) for create/load/save.
            path: Asset path for scene operations (default: "Assets/").
            build_index: Build index for load/build settings actions.

        Returns:
            Dictionary with results ('success', 'message', 'data').
        """
        bridge = get_unity_connection()

        params_dict = {
            "action": action.lower(),
            "name": name,
            "path": path,
            "buildIndex": build_index
        }

        params_dict = {k: v for k, v in params_dict.items() if v is not None}

        return bridge.send_command("manage_scene", params_dict) 
