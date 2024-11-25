

docker buildx build --platform linux/amd64 -t metafi/ok-coin-api:0.2.4 --load .

docker buildx build --platform linux/amd64,linux/arm64 -t metafi/ok-coin-api:0.2.4 --push .

docker push metafi/ok-coin-api:0.2.4