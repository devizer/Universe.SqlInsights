        image="$DOCKER_IMAGE_FULL"
        Say "Pull $image ..."
        try-and-retry docker pull $image || true
