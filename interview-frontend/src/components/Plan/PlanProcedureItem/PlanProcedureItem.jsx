import React, { useState, useEffect } from "react";
import ReactSelect from "react-select";
import {
  addUsersToProcedure,
  getAssignedUsers,
  getUsers,
} from "../../../api/api";
import { useParams } from "react-router-dom";

const PlanProcedureItem = ({ procedure }) => {
  const { id } = useParams();
  const [allUsers, setAllUsers] = useState([]);
  // const [uniqueAssignedUsers, setUniqueAssignedUsers] = useState([]);
  const [selectedUsers, setSelectedUsers] = useState([]);
  // console.log("PLANID", id);
  // console.log("PROCEDUREID", procedure.procedureId);
  // console.log(selectedUsers);

  useEffect(() => {
    const fetchData = async () => {
      try {
        // Fetch all users
        const allUsersData = await getUsers();
        const formattedAssignedUsers = allUsersData.map((user) => ({
          label: user.name,
          value: user.userId,
        }));
        // console.log(allUsersData)
        // console.log(formattedAssignedUsers)
        setAllUsers(formattedAssignedUsers);

        const { $values: assignedUsersData } = await getAssignedUsers(id, procedure.procedureId);

        if (assignedUsersData && assignedUsersData.length > 0) {
          const formattedAssignedUsers = assignedUsersData.map((user) => ({
            label: user.Name,
            value: user.UserId,
          }));
          setSelectedUsers(formattedAssignedUsers);
        } else {
          setSelectedUsers([]);
        }
      } catch (error) {
        console.error("Error fetching data:", error);
      }
    };
    fetchData();
  }, [id, procedure.procedureId]);

  const handleAssignUserToProcedure = async (selectedUsers) => {
    try {
      setSelectedUsers(selectedUsers);
      await addUsersToProcedure(
        Number(id),
        procedure.procedureId,
        selectedUsers.map((user) => user.value)
      );
    } catch (error) {
      console.error("Failed to add users to this procedure", error);
    }
  };

  return (
    <div className="py-2">
      <div>{procedure.procedureTitle}</div>

      <ReactSelect
        className="mt-2"
        placeholder="Select User to Assign"
        isMulti={true}
        options={allUsers}
        value={selectedUsers}
        onChange={(e) => handleAssignUserToProcedure(e)}
      />
    </div>
  );
};

export default PlanProcedureItem;
