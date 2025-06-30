import socket
import json
import logging
from dataclasses import dataclass
from typing import Dict, Any, Optional
from config import config

# Configure logging using settings from config
logging.basicConfig(
    level=getattr(logging, config.log_level),
    format=config.log_format
)
logger = logging.getLogger("unity-mcp-server")

@dataclass
class UnityConnection:
    """Manages the socket connection to the Unity Editor."""
    host: str = config.unity_host
    port: int = config.unity_port
    sock: Optional[socket.socket] = None  # Socket for Unity communication

    def connect(self) -> bool:
        """Establish a connection to the Unity Editor."""
        if self.sock:
            return True
        try:
            self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock.connect((self.host, self.port))
            logger.info(f"Connected to Unity at {self.host}:{self.port}")
            return True
        except Exception as e:
            logger.error(f"Failed to connect to Unity: {str(e)}")
            self.sock = None
            return False

    def disconnect(self):
        """Close the connection to the Unity Editor."""
        if self.sock:
            try:
                self.sock.close()
            except Exception as e:
                logger.error(f"Error disconnecting from Unity: {str(e)}")
            finally:
                self.sock = None

    def send_command(self, command_type: str, params: Dict[str, Any] = {}) -> Dict[str, Any]:
        """Send a command to Unity and return its response."""
        if not self.sock and not self.connect():
            raise ConnectionError("Not connected to Unity")
        if self.sock is None:
            raise ConnectionError("Socket is not connected")
        
        # Special handling for ping command
        if command_type == "ping":
            try:
                logger.debug("Sending ping to verify connection")
                self.sock.sendall(b"ping")
                response_data = self.receive_full_response(self.sock)
                if response_data is None:
                    raise ConnectionError("No response received from Unity")
                response = json.loads(response_data.decode('utf-8'))
                
                if response.get("status") != "success":
                    logger.warning("Ping response was not successful")
                    self.sock = None
                    raise ConnectionError("Connection verification failed")
                    
                return {"message": "pong"}
            except Exception as e:
                logger.error(f"Ping error: {str(e)}")
                self.sock = None
                raise ConnectionError(f"Connection verification failed: {str(e)}")
        
        # Normal command handling
        # Unity側のCommand.csはparametersをJSON文字列として期待しているため、ここで明示的にJSON文字列化する
        parameters_json = json.dumps(params or {}, ensure_ascii=False)
        command = {"type": command_type, "parameters": parameters_json}
        try:
            command_json = json.dumps(command, ensure_ascii=False)
            logger.info(f"Sending command: {command_type}")
            
            if self.sock is None:
                raise ConnectionError("Socket is not connected")
            self.sock.sendall(command_json.encode('utf-8'))
            
            response_data = self.receive_full_response(self.sock)
            if response_data is None:
                raise Exception("No response received from Unity")
            try:
                response = json.loads(response_data.decode('utf-8'))
            except json.JSONDecodeError as je:
                logger.error(f"JSON decode error: {str(je)}")
                raise Exception(f"Invalid JSON response from Unity: {str(je)}")
            
            if response.get("status") == "error":
                error_message = response.get("error") or response.get("message", "Unknown Unity error")
                logger.error(f"Unity error: {error_message}")
                raise Exception(error_message)
            
            return response.get("result", {})
        except Exception as e:
            logger.error(f"Communication error with Unity: {str(e)}")
            self.sock = None
            raise Exception(f"Failed to communicate with Unity: {str(e)}")

    def receive_full_response(self, sock, buffer_size=None) -> Optional[bytes]:
        """Receive a complete response from Unity, handling chunked data."""
        if buffer_size is None:
            buffer_size = config.buffer_size
            
        chunks = []
        sock.settimeout(config.connection_timeout)
        try:
            while True:
                chunk = sock.recv(buffer_size)
                if not chunk:
                    if not chunks:
                        raise Exception("Connection closed before receiving data")
                    break
                chunks.append(chunk)
                
                # Process the data received so far
                data = b''.join(chunks)
                decoded_data = data.decode('utf-8')
                
                # Check if we've received a complete response
                try:
                    # Special case for ping-pong
                    if decoded_data.strip().startswith('{"status":"success","result":{"message":"pong"'):
                        logger.debug("Received ping response")
                        return data
                    
                    # Validate JSON format
                    json.loads(decoded_data)
                    
                    # If we get here, we have valid JSON
                    logger.info(f"Received complete response ({len(data)} bytes)")
                    return data
                except json.JSONDecodeError:
                    # We haven't received a complete valid JSON response yet
                    continue
                except Exception as e:
                    logger.warning(f"Error processing response chunk: {str(e)}")
                    # Continue reading more chunks as this might not be the complete response
                    continue
        except socket.timeout:
            logger.warning("Socket timeout during receive")
            raise Exception("Timeout receiving Unity response")
        except Exception as e:
            logger.error(f"Error during receive: {str(e)}")
            raise

# Global Unity connection
_unity_connection = None

def get_unity_connection() -> UnityConnection:
    """Retrieve or establish a persistent Unity connection."""
    global _unity_connection
    if _unity_connection is not None:
        try:
            # Try to ping with a short timeout to verify connection
            result = _unity_connection.send_command("ping")
            # If we get here, the connection is still valid
            logger.debug("Reusing existing Unity connection")
            return _unity_connection
        except Exception as e:
            logger.warning(f"Existing connection failed: {str(e)}")
            try:
                _unity_connection.disconnect()
            except:
                pass
            _unity_connection = None
    
    # Create a new connection
    logger.info("Creating new Unity connection")
    _unity_connection = UnityConnection()
    if not _unity_connection.connect():
        _unity_connection = None
        raise ConnectionError("Could not connect to Unity. Ensure the Unity Editor and MCP Bridge are running.")
    
    try:
        # Verify the new connection works
        _unity_connection.send_command("ping")
        logger.info("Successfully established new Unity connection")
        return _unity_connection
    except Exception as e:
        logger.error(f"Could not verify new connection: {str(e)}")
        try:
            _unity_connection.disconnect()
        except:
            pass
        _unity_connection = None
        raise ConnectionError(f"Could not establish valid Unity connection: {str(e)}")
