extends Control

# from one of my other projects
func _ready() -> void:
	if DisplayServer.is_touchscreen_available():
		self.show()
