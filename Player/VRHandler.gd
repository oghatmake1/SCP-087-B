extends Node3D

var xrinterface = XRServer.find_interface("OpenXR")
# Keep track of the camera's position on the previous frame
var last_camera_pos: Vector3 = Vector3.ZERO
var last_camera_yaw: float = 0.0

func _ready():
	if not (xrinterface and xrinterface.is_initialized()):
		$Camera3D.make_current()
	else:
		get_viewport().use_xr = true
		
	# Initialize the position tracking loop
	var camera = $XROrigin3D/XRCamera3D
	last_camera_pos = camera.global_position
	
func _process(_delta: float) -> void:
	$Camera3D.transform = $XROrigin3D/XRCamera3D.transform 
	if xrinterface and xrinterface.is_initialized():
		var player = $".."
		var origin = $XROrigin3D
		var camera = $XROrigin3D/XRController3D #$XROrigin3D/XRCamera3D
		# the following lines are temporary
		var delta_yaw = angle_difference(last_camera_yaw, camera.rotation.y)
		last_camera_yaw = camera.rotation.y
		"""
		# 1. Calculate how much the camera moved since the last frame
		var movement_delta_x = camera.global_position.x - last_camera_pos.x
		var movement_delta_z = camera.global_position.z - last_camera_pos.z
		# 2. Add that frame-by-frame change to the Player root
		player.global_position.x += movement_delta_x
		player.global_position.z += movement_delta_z

		# 3. Pull the XROrigin back by the exact same frame change amount.
		# This shifts the origin workspace beneath the player without moving the camera in global space.
		origin.global_position.x -= movement_delta_x
		origin.global_position.z -= movement_delta_z
		last_camera_pos = camera.global_position
		"""
		player.rotation.y += delta_yaw 
		origin.rotation.y -= delta_yaw 
		
		print($XROrigin3D/XRController3D.get_vector2("stick"))
		
