docker build -t otm_image .
docker run -it --rm -p 5000:80 --name otm otm_image