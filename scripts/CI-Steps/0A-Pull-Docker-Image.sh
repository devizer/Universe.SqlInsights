        image="devizervlad/iis-net4x-net35:$SQL_IMAGE_TAG"
        Say "Pull $image ..."
        try-and-retry docker pull $image || true
