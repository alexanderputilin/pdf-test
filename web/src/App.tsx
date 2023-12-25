import React, {useEffect, useRef} from 'react';

import Container from 'react-bootstrap/Container';
import {FileMessage, FileMessageInfo} from "./components/fileMessage";
import * as signalR from "@microsoft/signalr";


const App: React.FC = () => {
    const [files, setFiles] = React.useState<FileMessageInfo[]>([]);
    const [selectedFile, setSelectedFile] = React.useState<File | null>(null);
    const [uploading, setUploading] = React.useState<boolean>(false);
    const fileInput = useRef<HTMLInputElement>(null);
    const ws = useRef<signalR.HubConnection | null>(null);
    useEffect(() => {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/api/notifications")
            .withAutomaticReconnect()
            .build();
        connection.onreconnected(async () => {
            for (const file of files) {
                await connection.send("subscribeToFile", file.id);
            }
        });
        connection.start().then(() => {
            ws.current = connection;
            ws.current?.on("TaskResult", (file: FileMessageInfo) => {
                setFiles((files) => {
                    const index = files.findIndex((f) => f.id === file.id);
                    if (index > -1) {
                        files[index] = file;
                    }
                    return [...files];
                });
            });

        }).catch((err) => {
            console.log(err.toString());

        })
    }, [])

    return (

        <Container className="vh-100">
            <div className="row d-flex justify-content-center vh-100">
                <div className="col-md-8 col-lg-7 col-xl-6">

                    <div className="card vh-100">
                        <div
                            className="card-header d-flex justify-content-between align-items-center p-3 bg-dark text-white border-bottom-0">
                            <p className="mb-0 fw-bold">Pdf test</p>
                        </div>
                        <div className="card-body overflow-scroll">
                            {
                                files.map(file => (<FileMessage key={file.id} file={file} onRemoveClick={async (id) => {
                                    await fetch(`/api/delete/${file.id}`, {
                                        method: "DELETE",
                                    });
                                    setFiles(files.filter((f) => f.id !== id));
                                }}/>))
                            }
                        </div>

                        <div className="d-flex">
                            <div className="flex-grow-1">
                                <input className="form-control" onChange={(e) => {

                                    if (e.target.files) {
                                        setSelectedFile(e.target.files[0])
                                    }

                                }} ref={fileInput} accept={".htm,.html"} type="file" disabled={uploading}/>
                            </div>
                            <div className="flex-grow-0">
                                {selectedFile && (
                                    uploading ?
                                        <div className="spinner-border" role="status">
                                            <span className="visually-hidden">Processing...</span>
                                        </div> :
                                        <button type="button" className="btn" onClick={async () => {
                                            const formData = new FormData();
                                            formData.append("file", selectedFile as File);
                                            setUploading(true);
                                            try {
                                                // You can write the URL of your server or any other endpoint used for file upload
                                                const result = await fetch("/api/upload", {
                                                    method: "POST",
                                                    body: formData,
                                                });

                                                const data = await result.json();

                                                setFiles([...files, data as FileMessageInfo]);
                                                ws.current?.send("subscribeToFile", data.id);

                                            } catch (error) {
                                                alert("Error uploading the file");
                                            } finally {
                                                setUploading(false);
                                                setSelectedFile(null);
                                                if (fileInput.current) {
                                                    fileInput.current.value = "";
                                                }
                                            }
                                        }}>Upload</button>)}
                            </div>
                        </div>
                    </div>

                </div>

            </div>


        </Container>

    );
};

export default App;
