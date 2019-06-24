namespace WebSiteMonitor.Service.Models.Response {
    public class ErrorResponse : BaseResponse {
        public string error { get; set; }
        public string url { get; set; }

        public ErrorResponse(string errorMessage, string loginUrl) {
            error = errorMessage;
            url = loginUrl;
        }

        public ErrorResponse(string errorMessage) {
            error = errorMessage;
        }
    }
}
