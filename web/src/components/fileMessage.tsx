import React from "react";

export interface FileMessageInfo {
    id: string;
    name: string;
    url: string;
    state: string;

}

interface FileMessageProps {
    file: FileMessageInfo;
    onRemoveClick: RemoveCallback;
}

interface RemoveCallback {
    (id: string): void
}

export const FileMessage: React.FC<FileMessageProps> = ({
                                                            file,
                                                            onRemoveClick
                                                        }) => (
    <div className="d-flex flex-row justify-content-end mb-4">
        <div className="p-3 me-3 border">
            <h5 className="card-title">{file.name}</h5>
            {file.state == "Done" ? (
                    <a className="btn btn-link" href={`/api/download/${file.id}`} target={"_blank"}>Download</a>) :
                file.state == "Error" ? (<div className="alert alert-danger" role="alert">
                        File processing error
                    </div>) :
                    (<div>
                        <div className="spinner-border" role="status">
                            <span className="visually-hidden">Processing...</span>
                        </div>
                    </div>)
            }

            <button type="button" className="btn btn-danger" onClick={async () => {
                onRemoveClick(file.id);
            }}>Remove
            </button>
        </div>

    </div>);


