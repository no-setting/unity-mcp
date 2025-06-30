"""
Defines the manage_editor tool for editor control in Unity.
"""
from typing import Dict, Any, Optional
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_manage_editor_tools(mcp: FastMCP):
    """Registers the manage_editor tool with the MCP server."""

    @mcp.tool()
    def manage_editor(
        ctx: Context,
        action: str,
        wait_for_completion: Optional[bool] = None,
        tool_name: Optional[str] = None,
        tag_name: Optional[str] = None,
        layer_name: Optional[str] = None,
    ) -> Dict[str, Any]:
        """Controls and queries the Unity editor's state and settings.

        Args:
            ctx: The MCP context.
            action: Operation (e.g., 'play', 'pause', 'get_state', 'set_active_tool', 'add_tag').
            wait_for_completion: Optional. If True, waits for certain actions.
            tool_name: The name of the tool to set active.
            tag_name: The name of the tag to add or check.
            layer_name: The name of the layer to add or check.

        Returns:
            Dictionary with operation results ('success', 'message', 'data').
        """
        bridge = get_unity_connection()

        params_dict = {
            "action": action.lower(),
            "waitForCompletion": wait_for_completion,
            "toolName": tool_name,
            "tagName": tag_name,
            "layerName": layer_name,
        }

        params_dict = {k: v for k, v in params_dict.items() if v is not None}

        return bridge.send_command("manage_editor", params_dict) 
