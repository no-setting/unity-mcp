"""
Defines the manage_script tool for C# script management in Unity.
"""
from typing import Dict, Any
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_manage_script_tools(mcp: FastMCP):
    """Registers the manage_script tool with the MCP server."""

    @mcp.tool()
    def manage_script(
        ctx: Context,
        action: str,
        name: str,
        path: str = None,
        contents: str = None,
        script_type: str = None,
        namespace: str = None
    ) -> Dict[str, Any]:
        """Manages C# scripts in Unity (create, read, update, delete).
        Make reference variables public for easier access in the Unity Editor.

        Args:
            ctx: The MCP context.
            action: Operation ('create', 'read', 'update', 'delete').
            name: Script name (no .cs extension).
            path: Asset path (default: "Assets/Scripts").
            contents: C# code for 'create'/'update'.
            script_type: Type hint (e.g., 'MonoBehaviour').
            namespace: Script namespace.

        Returns:
            Dictionary with results ('success', 'message', 'data').
        """
        
        # Get the Unity connection
        bridge = get_unity_connection()

        # Prepare parameters for the C# handler
        params_dict = {
            "action": action.lower(),
            "name": name,
            "path": path,
            "contents": contents,
            "scriptType": script_type,
            "namespace": namespace
        }

        # Remove None values to avoid sending unnecessary nulls
        params_dict = {k: v for k, v in params_dict.items() if v is not None}

        # Forward the command using the bridge's send_command method
        return bridge.send_command("manage_script", params_dict)
