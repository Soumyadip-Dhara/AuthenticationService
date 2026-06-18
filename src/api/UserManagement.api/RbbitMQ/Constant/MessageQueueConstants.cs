namespace UserManagement.RbbitMQ.Constant
{
    public static class MessageQueueConstants
    {
        //Ack Consumer 
        public const string DOCUMENT_STROAGE_ACK = "usermanagement_documentstorage_application_map_ack";
        public const string ABC_ACK = "abc_ack";
        public const string DOCUMENT_STROAGE_SEND_CLIENT_SECRET = "usermanagement_documentstorage_application_map";
        public const string USER_REGISTRATION_QUEUE = "wbjit_um_user";
        public const string USER_REGISTRATION_QUEUE_ACK = "wbjit_um_user_ack";
        public const string UM_WBJIT_USER = "um_wbjit_user";
        public const string UM_WBJIT_USER_ACK = "um_wbjit_user_ack";
        public const string WBJIT_UM_SLS_AGENCY = "wbjit_um_sls_agency";
        public const string WBJIT_UM_SLS_AGENCY_ACK = "wbjit_um_sls_agency_ack";


        public const string WBJIT_UM_SNAPSHOT_REQUEST = "wbjit_um_snapshot_request";
        
        public const string WBJIT_UM_USER_COMPARISON_REQUEST_ACK = "wbjit_um_user_comparison_ack";

        public const string UM_MASTER_TREASURY = "master_um_treasury";

        public const string UM_MASTER_DDO = "master_um_ddo";

        public const string MASTER_ACK = "master_ack";
    }
       
}
