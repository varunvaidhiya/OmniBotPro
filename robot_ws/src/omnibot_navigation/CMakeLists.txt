cmake_minimum_required(VERSION 3.8)
project(omnibot_navigation)

if(CMAKE_COMPILER_IS_GNUCXX OR CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  add_compile_options(-Wall -Wextra -Wpedantic)
endif()

# find dependencies
find_package(ament_cmake REQUIRED)

# Install launch files and config
install(DIRECTORY
  launch
  config
  maps
  DESTINATION share/${PROJECT_NAME}/
)

# Install Python scripts so they are discoverable as ROS 2 executables
install(PROGRAMS
  scripts/waypoint_navigator.py
  scripts/test_autonomous.py
  DESTINATION lib/${PROJECT_NAME}
)

if(BUILD_TESTING)
  find_package(ament_lint_auto REQUIRED)
  # Skip copyright and cpplint checks — launch files / configs have no
  # copyright headers (consistent with omnibot_bringup / omnibot_description).
  set(ament_cmake_copyright_FOUND TRUE)
  set(ament_cmake_cpplint_FOUND TRUE)
  ament_lint_auto_find_test_dependencies()
endif()

ament_package()
