

docker buildx build --platform linux/amd64 -t metafi/ok-coin-api:0.2.1 --load .

docker buildx build --platform linux/amd64,linux/arm64 -t metafi/ok-coin-api:0.2.1 --push .

docker push metafi/ok-coin-api:0.2.1