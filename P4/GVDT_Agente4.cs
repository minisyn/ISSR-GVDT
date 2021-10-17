using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Propuestas de mejoras:
//===============================================
//REALIZAR UNA FUNCION PARA LA COMPROBACIÓN DE ERRORES DE FORMA AUTOMÁTICA (JUGAR CON HERENCIAS)
//OJO, PODEMOS REALIZAR UNA FUNCIÓN PARA PREVEER LA RUTA DE UN AGENTE SI HA COGIDO UN AGENTE Y EVITAR COLISIONES
//AL MENOS CON EL MISMO EQUIPO y que nos avisen de que han cogido (comunicación)


public class GVDT_Agente4 : ISSR_Agent
{
    public override void Start()
    {
        //  Debug.LogFormat("{0}: Comienza", Myself.Name);
    }
    // Update is called once per frame
    public override IEnumerator Update()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            //En este punto generaremos un evento de tiempo
            current_event = ISSREventType.onTickElapsed;
            current_state = AgentStateMachine();
        }
    }
    ISSRState AgentStateMachine() // Función principal de máquina de estados
    {
        ISSRState next_state = current_state; // estado de salida, en principio igual

        switch (current_state) // Según el estado
        {
            case ISSRState.Idle:

                next_state = SF_Idle();
                break;

            case ISSRState.GoingToGripSmallStone:

                next_state = SF_GoingToGripSmallStone();
                break;

            case ISSRState.GoingToGoalWithSmallStone:

                next_state = SF_GoingToGoalWithSmallStone();
                break;

            case ISSRState.WaitforNoStonesMoving:
                //Situado entre ir a por una piedra e ir a la meta
                next_state = SF_WaitforNoStonesMoving();
                break;

            case ISSRState.AvoidingObstacle:

                next_state = SF_AvoidingObstacle();
                break;

            case ISSRState.End:

                break;

            default:

                Debug.LogErrorFormat("{0}: estado {1} no considerado", Myself.Name,
                    current_state);
                break;
        }

        if (current_state != next_state) // Si ha cambiado el estado
        {
            Debug.LogWarningFormat("{0}: Estado '{1}'-->'{2}' por evento '{3}'", Myself.Name,
                           current_state, next_state, current_event);
        }

        return next_state;
    }

    ISSRState SF_Idle()  // SF “State Function”
    {

        ISSRState next_state = current_state;
        switch (current_event) // Según el evento
        {
            case ISSREventType.onTickElapsed:
                //coger de la lista el objeto más cercano al agente del tipo SmallStone (Objeto focuseado)
                focus_object = ISSRHelp.GetCloserToMeObjectInList(this, Valid_Small_Stones, ISSR_Type.SmallStone);
                if (focus_object != null)
                {
                    // P3 --> next_state = processStone();
                    next_state = GetSStone(focus_object);
                }
                else
                {
                    Debug.LogFormat("{0}: no reconozco más piedras pequeñas", Myself.Name);
                    next_state = ISSRState.End;
                }
                break;
            case ISSREventType.onEnterSensingArea:

                break;
            default:
                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'",
                   Myself.Name, current_event, current_state);
                break;
        }

        return next_state;
    }

    ISSRState SF_WaitforNoStonesMoving()
    {

        ISSRState next_state = current_state;
        switch (current_event)
        {
            case ISSREventType.onTickElapsed:
                if (iMovingStonesInMyTeam() == 0)
                {
                    acGotoLocation(iMyGoalLocation());
                    if (acCheckError())
                    {
                        next_state = ISSRState.Error;
                    }
                    else
                    {
                        Debug.LogFormat("{0}: De camino a la meta", Myself.Name);
                        next_state = ISSRState.GoingToGoalWithSmallStone;
                    }
                }
                break;

            case ISSREventType.onUngrip:
                //DUDAAAAAA: Esto se hace reealmente así?
                SStoneIsAvaiable(focus_object, true);
                next_state = GetSStone(focus_object);
                break;
            default:
                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'",
                      Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }


    ISSRState SF_GoingToGripSmallStone()
    {

        ISSRState next_state = current_state;
        switch (current_event)
        {
            case ISSREventType.onTickElapsed:
                //Comprobar que la piedra que voy a buscar está realmente ahí (una vez entra dentro del rango de visión) -- con refactoring podemos ahorrar procesamiento
                next_state = GetSStone(focus_object);
                break;
            case ISSREventType.onGripSuccess:

                SStoneIsAvaiable(focus_object, false);
                next_state = ISSRState.WaitforNoStonesMoving;
                break;

            case ISSREventType.onEnterSensingArea:

                if (object_just_seen.Equals(focus_object)) // veo justo la piedra que 'recuerdo'
                {
                    GetSStone(focus_object); // intento agarrar esa piedra pequeña
                }
                break;

            case ISSREventType.onCollision:

                if (colliding_object.Equals(focus_object))
                {
                    next_state = GetSStone(focus_object);
                    if (acCheckError())
                    {
                        next_state = ISSRState.Error;
                    }
                }
                else
                {
                    next_state = processCollision();
                }
                break;

            case ISSREventType.onDestArrived:
                next_state = ISSRState.Idle;
                break;
            case ISSREventType.onGripFailure:
                SStoneIsAvaiable(focus_object, false);
                next_state = ISSRState.Idle;

                break;
            case ISSREventType.onObjectLost:
                SStoneIsAvaiable(focus_object, false);
                next_state = ISSRState.Idle;
                break;

            default:

                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'",
                      Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    ISSRState SF_GoingToGoalWithSmallStone()
    {

        ISSRState next_state = current_state;
        switch (current_event)
        {
            case ISSREventType.onTickElapsed:
                break;
            case ISSREventType.onGObjectScored:

                next_state = ISSRState.Idle;
                Debug.LogFormat("{0}: Piedra {1} dejada en la meta", Myself.Name, focus_object.Name);
                break;

            case ISSREventType.onCollision:

                next_state = processCollision();
                break;

            case ISSREventType.onGObjectCollision:

                next_state = processCollision();
                break;

            default:

                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'",
                      Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    ISSRState SF_AvoidingObstacle()
    {

        ISSRState next_state = current_state;
        switch (current_event)
        {
            case ISSREventType.onTickElapsed:
                break;
            case ISSREventType.onCollision:

                next_state = processCollision();
                break;

            case ISSREventType.onGObjectCollision:

                next_state = processCollision();
                break;

            case ISSREventType.onDestArrived:

                next_state = ResumeAfterCollision();
                break;

            case ISSREventType.onGObjectScored:

                next_state = SF_Idle();
                break;

            default:

                Debug.LogWarningFormat("{0} Evento '{1}' no considerado en estado '{2}'",
                      Myself.Name, current_event, current_state);
                break;
        }
        return next_state;
    }

    private ISSRState processStone()
    {
        ISSRState next_state;

        if (oiSensable(focus_object))
        {
            Debug.LogFormat("{0}: trata de coger {1}", Myself.Name, focus_object.Name);
            acGripObject(focus_object);

        }
        else
        {
            Debug.LogFormat("{0}: {1} fuera de vista voy a su última posición", Myself.Name, focus_object.Name);
            acGotoLocation(oiLastLocation(focus_object));
        }

        if (acCheckError())
        {
            next_state = ISSRState.Error;
        }
        else
        {
            next_state = ISSRState.GoingToGripSmallStone;
        }

        return next_state;
    }

    private void SStoneIsAvaiable(ISSR_Object stone, bool avaiable)
    {

        //oiGrippingAgents(obj) nos indica el número de agentes que agarran una piedr
        if (avaiable)
        {
            //disponible
            if (!Valid_Small_Stones.Contains(stone))
            {
                Valid_Small_Stones.Add(stone);
                Debug.LogFormat("{0}: nueva piedra pequeña {1}, anotada", Myself.Name, stone.Name);
            } //no está en la lista --> añadir
        }
        else
        { //No disponible
            if (Valid_Small_Stones.Contains(stone))
            {
                Valid_Small_Stones.Remove(stone);
                Debug.LogFormat("{0}: piedra pequeña {1} eliminada", Myself.Name, stone.Name);
            } //está en la lista --> eliminar
        }
    }

    //NECESITA REFACTORING
    private ISSRState GetSStone(ISSR_Object stone)
    {

        ISSRState next_state;

        double distancia = ( stone.LastLocation - oiLocation (Myself)).magnitude;

        if (oiSensable(stone))
        {
            if (oiGrippingAgents(stone) > 0)
            {
                SStoneIsAvaiable(stone, false);
                acStop();
                if (acCheckError())
                {
                    next_state = ISSRState.Error;
                }
                next_state = ISSRState.Idle;
            }
            else
            {
                acGripObject(stone);
                if (acCheckError())
                {
                    next_state = ISSRState.Error;
                }
                next_state = ISSRState.GoingToGripSmallStone;
            }
        }
        else
        {
            if (distancia < iSensingRange())
            {
                SStoneIsAvaiable(stone, false);
                acStop();
                if (acCheckError())
                {
                    next_state = ISSRState.Error;
                }
                next_state = ISSRState.Idle;

            }
            else
            {
                acGotoLocation(oiLastLocation(stone));
                if (acCheckError())
                {
                    next_state = ISSRState.Error;
                }
                next_state = ISSRState.GoingToGripSmallStone;
            }
        }

        return next_state;

    }

    private ISSRState processCollision()
    {
        ISSRState next_state = current_state;

        switch (current_state)
        {
            case ISSRState.GoingToGripSmallStone:

                last_state = current_state;
                next_state = acGotoSafeLocation();
                break;

            case ISSRState.GoingToGoalWithSmallStone:

                last_state = current_state;
                next_state = acGotoSafeLocation();
                break;

            case ISSRState.AvoidingObstacle:

                next_state = acGotoSafeLocation();
                break;

            default:

                Debug.LogErrorFormat("{0}, estado {1} no considerado al colisionar", Myself.Name, current_state);
                break;
        }

        return next_state;
    }

    ISSRState ResumeAfterCollision() // Versi�n de Pr�ctica 3, completar en las siguientes
    { // Continuar con lo que se estaba haciendo en el momento de la colisi�n.
        ISSRState next_state = current_state;

        switch (last_state)
        {
            case ISSRState.GoingToGripSmallStone:

                next_state = GetSStone(focus_object);  // Volver a pedir coger piedra o ir a su lugar
                break;

            case ISSRState.GoingToGoalWithSmallStone:

                if (iMovingStonesInMyTeam() == 0)
                {
                    acGotoLocation(iMyGoalLocation());  // volver a pedir ir a la meta
                    if (acCheckError())
                    {
                        next_state = ISSRState.Error;
                    }
                    else
                    {
                        next_state = ISSRState.GoingToGoalWithSmallStone;
                    }
                }
                else
                {
                    next_state = ISSRState.WaitforNoStonesMoving;
                }
                break;

            default:
                Debug.LogErrorFormat("{0}, estado {1} no considerado al volver de colisión", Myself.Name, last_state);
                break;
        }
        return next_state;
    }

    private ISSRState acGotoSafeLocation()
    {

        ISSRState next_state;
        acGotoLocation(ISSRHelp.CalculateSafeLocation(this, colliding_object));
        if (acCheckError())
        {
            next_state = ISSRState.Error;
        }
        else
        {
            next_state = ISSRState.AvoidingObstacle;
        }
        return next_state;
    }

    public override void onEnterSensingArea(ISSR_Object obj)
    {
        object_just_seen = obj;

        if ((obj.type == ISSR_Type.SmallStone) && !Valid_Small_Stones.Contains((obj)))
        {
            SStoneIsAvaiable(obj, true);
            if (oiGrippingAgents(obj) == 0)
            {
                SStoneIsAvaiable(obj, true);
                GetSStone(obj);
            }
            else
            {
                SStoneIsAvaiable(obj, false);
            }
        }
        current_state = AgentStateMachine();
    }

    //Podemos programar las acciones
    public override void onGripSuccess(ISSR_Object obj_gripped)
    {
        Debug.LogFormat("{0}: agarra {1}, va para la meta", Myself.Name, obj_gripped.Name);
        current_state = AgentStateMachine();
    }

    public override void onGObjectScored(ISSR_Object stone_that_scored)
    {
        Debug.LogFormat("{0}: piedra {1} en meta", Myself.Name, stone_that_scored.Name);
        current_state = AgentStateMachine();
    }

    public override void onCollision(ISSR_Object obj_that_collided_with_me)
    {
        //Objeto contra el propio agente
        colliding_object = obj_that_collided_with_me;
        Debug.LogFormat("{0}: he recibido una colisión con {1}", Myself.Name, obj_that_collided_with_me.Name);
        current_state = AgentStateMachine();
    }

    public override void onGObjectCollision(ISSR_Object obj_that_collided_with_gripped_obj)
    {
        //Objeto contra el objeto agarrado
        colliding_object = obj_that_collided_with_gripped_obj;
        Debug.LogFormat("{0}: he recibido una colisión con {1}", Myself.Name, colliding_object.Name);
        current_state = AgentStateMachine();
    }

    public override void onDestArrived()
    {
        switch (current_state)
        {
            case ISSRState.AvoidingObstacle:
                current_state = ResumeAfterCollision();
                Debug.LogFormat("{0}: he llegado al destino después de una colisión", Myself.Name);
                break;
            case ISSRState.GoingToGripSmallStone:
                Debug.LogFormat("{0}: he llegado al destino buscando: {1}", Myself.Name, focus_object.Name);
                SStoneIsAvaiable(focus_object, false);
                current_state = ISSRState.Idle;
                break;
            default:
                Debug.LogFormat("{0}: He llegado al destino", Myself.Name);
                break;
        }

    }

    public override void onUngrip(ISSR_Object ungripped_object)
    {
        colliding_object = ungripped_object;
        Debug.LogFormat("{0}: he soltado la piedra después de colisión con: {1}", Myself.Name, colliding_object.Name);
        current_state = AgentStateMachine();
    }

    public override void onGripFailure(ISSR_Object obj_failed)
    {
        Debug.LogFormat("{0}: Esta piedra({1}) ya tiene otro agente sujetandola", Myself.Name, obj_failed.Name);
        current_state = AgentStateMachine();
    }

    public override void onObjectLost(ISSR_Object obj_missed)
    {
        Debug.LogFormat("{0}: Esta piedra({1}) se ha perdido", Myself.Name, obj_missed.Name);
        current_state = AgentStateMachine();
    }
}
