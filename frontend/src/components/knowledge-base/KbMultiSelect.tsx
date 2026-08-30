"use client";

import { useEffect, useState } from "react";
import { Autocomplete, Chip, TextField } from "@mui/material";
import StorageIcon from "@mui/icons-material/Storage";
import { knowledgeBaseApi } from "@/services/api";
import type { KnowledgeBase } from "@/types";

interface Props {
  value: string[];
  onChange: (ids: string[]) => void;
}

export function KbMultiSelect({ value, onChange }: Props) {
  const [options, setOptions] = useState<KnowledgeBase[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    knowledgeBaseApi
      .list()
      .then((res) => setOptions(res.knowledge_bases))
      .finally(() => setLoading(false));
  }, []);

  const selected = options.filter((kb) => value.includes(kb.id));

  return (
    <Autocomplete
      multiple
      loading={loading}
      options={options}
      value={selected}
      getOptionLabel={(kb) => kb.name}
      isOptionEqualToValue={(a, b) => a.id === b.id}
      onChange={(_, newValue) => onChange(newValue.map((kb) => kb.id))}
      noOptionsText="Nenhuma base de conhecimento criada ainda."
      renderValue={(tagValue, getItemProps) =>
        tagValue.map((kb, index) => {
          const { key, ...itemProps } = getItemProps({ index });
          return (
            <Chip
              {...itemProps}
              key={key}
              label={kb.name}
              size="small"
              icon={<StorageIcon />}
              color="primary"
              variant="outlined"
            />
          );
        })
      }
      renderInput={(params) => (
        <TextField
          {...params}
          placeholder={selected.length === 0 ? "Adicionar base de conhecimento..." : undefined}
          size="small"
        />
      )}
    />
  );
}
