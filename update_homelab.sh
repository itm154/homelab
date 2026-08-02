#!/bin/bash
set -e

# Target directories in order
dirs=(
  "networking"
  "qbittorrent"
  "arr"
  "seerr"
  "jellyfin"
  "bentopdf"
  "fileflows"
)

homelab_dir="/home/itm154/Repository/Homelab"

echo "=== Starting Homelab Update Process ==="
echo "Current Time: $(date)"
echo ""

# Function to pull images with retry logic
pull_with_retry() {
  local retries=3
  local count=0
  local success=0
  
  until [ $count -ge $retries ]
  do
    echo "=> Pulling latest images (Attempt $((count+1))/$retries)..."
    if docker compose pull --ignore-pull-failures; then
      success=1
      break
    else
      count=$((count+1))
      echo "WARNING: docker compose pull failed. Retrying in 5 seconds..."
      sleep 5
    fi
  done
  
  if [ $success -ne 1 ]; then
    echo "ERROR: Failed to pull images after $retries attempts."
    exit 1
  fi
}

for dir in "${dirs[@]}"; do
  full_path="${homelab_dir}/${dir}"
  echo "--------------------------------------------------"
  echo "Updating and starting service in: ${dir}"
  echo "--------------------------------------------------"
  
  if [ -d "$full_path" ]; then
    cd "$full_path"
    
    # Try to pull with retry
    pull_with_retry
    
    # Check if compose file has build block
    if grep -q "build:" docker-compose.yml 2>/dev/null || grep -q "build:" compose.yml 2>/dev/null; then
      echo "=> Build block detected. Building with latest base images..."
      docker compose build --pull
    fi
    
    echo "=> Starting/Recreating containers..."
    docker compose up -d
    
    # If we just updated networking, sleep a bit to let AdGuardHome DNS start up
    if [ "$dir" = "networking" ]; then
      echo "=> Cooldown: Sleeping 10 seconds for DNS (AdGuardHome) to settle..."
      sleep 10
    fi
    
    echo "=> Done with ${dir}."
  else
    echo "ERROR: Directory ${full_path} does not exist!"
  fi
  echo ""
done

echo "=== Homelab Update Process Completed Successfully! ==="
