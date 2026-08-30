"use client";

import { Dialog, DialogContent, DialogTitle, IconButton } from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import type { KnowledgeBase } from "@/types";
import { KnowledgeBaseForm } from "./KnowledgeBaseForm";

interface Props {
  onCreated: (kb: KnowledgeBase) => void;
  onClose: () => void;
}

export function KnowledgeBaseModal({ onCreated, onClose }: Props) {
  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        Nova base de conhecimento
        <IconButton onClick={onClose} size="small">
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <KnowledgeBaseForm onCreated={onCreated} onCancel={onClose} />
      </DialogContent>
    </Dialog>
  );
}
