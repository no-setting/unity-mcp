"""
Defines the read_console tool for reading console logs in Unity.
"""
from typing import Dict, Any, Optional, List
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_read_console_tools(mcp: FastMCP):
    """Registers the read_console tool with the MCP server."""

    @mcp.tool()
    def read_console(
        ctx: Context,
        action: str = "get",
        types: Optional[List[str]] = None,
        filter_text: Optional[str] = None,
        clear: bool = False # Legacy, for backward compatibility
    ) -> Dict[str, Any]:
        """Gets messages from or clears the Unity Editor console.

        Args:
            ctx: The MCP context.
            action: Operation ('get' or 'clear'). Defaults to 'get'.
            types: Message types to get ('error', 'warning', 'log'). Defaults to all.
            filter_text: Text filter for messages.
            clear: If True, clears the console after getting messages. Deprecated in favor of action='clear'.

        Returns:
            Dictionary with results. For 'get', includes 'data' (messages).
        """
        bridge = get_unity_connection()
        
        # Handle the legacy `clear` parameter
        effective_action = "clear" if clear else action.lower()

        params_dict = {
            "action": effective_action,
            "types": types,
            "filterText": filter_text,
        }
        
        params_dict = {k: v for k, v in params_dict.items() if v is not None}
        
        return bridge.send_command("read_console", params_dict) 
