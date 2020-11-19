
#define MAX_FORCE_STEPS 5
#define MAX_SPECIES 8

#define FRICTION 0.01
#define DAMP_CONSTANT (1.0 - FRICTION)

#define RADIUS 0.01

#define MAX_SOCIAL_RANGE (RADIUS * 20)
#define MAX_COLLISION_FORCE 2
#define MAX_SOCIAL_FORCE 0.5

struct Particle {
  float3 position;
  float3 prevPosition;
  float3 color;
};
